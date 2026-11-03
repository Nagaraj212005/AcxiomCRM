using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public sealed class CrmService : ICrmService
{
    private readonly ICustomerRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<CrmService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CrmService(
        ICustomerRepository repository,
        IAuditService auditService,
        ILogger<CrmService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<Lead>> GetLeadsAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var leads = _repository.Leads;
        if (!viewAll)
        {
            leads = leads.Where(lead => lead.AssignedTo == null || lead.AssignedTo == userId);
        }

        return await leads.OrderByDescending(lead => lead.CreatedDate).ToListAsync(cancellationToken);
    }

    public async Task<Lead?> GetLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.FindLeadByIdAsync(id, cancellationToken);
        return lead is not null && CanAccess(lead.AssignedTo, userId, viewAll) ? lead : null;
    }

    public async Task<Lead?> SaveLeadAsync(Lead input, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        if (!id.HasValue && input.Status == LeadStatus.Converted)
        {
            return null;
        }

        Lead lead;
        string? oldValue = null;
        if (id.HasValue)
        {
            var existing = await _repository.FindLeadByIdAsync(id.Value, cancellationToken);
            if (existing is null || !CanAccess(existing.AssignedTo, userId, viewAll))
            {
                return null;
            }
            if ((existing.Status == LeadStatus.Converted && input.Status != LeadStatus.Converted) ||
                (existing.Status != LeadStatus.Converted && input.Status == LeadStatus.Converted))
            {
                return null;
            }
            lead = existing;
            oldValue = $"{lead.LeadCode}: {lead.Status}, priority {lead.Priority}, expected value {lead.ExpectedValue}.";

            lead.Name = input.Name.Trim();
            lead.Email = input.Email.Trim();
            lead.Phone = input.Phone.Trim();
            lead.Company = input.Company.Trim();
            lead.Source = input.Source;
            lead.Status = input.Status;
            lead.Priority = input.Priority;
            lead.ExpectedValue = input.ExpectedValue;
            lead.Notes = input.Notes.Trim();
            lead.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            lead = input;
            lead.Id = 0;
            lead.LeadCode = $"LD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
            lead.Name = input.Name.Trim();
            lead.Email = input.Email.Trim();
            lead.Phone = input.Phone.Trim();
            lead.Company = input.Company.Trim();
            lead.Notes = input.Notes.Trim();
            lead.AssignedTo = userId;
            lead.CreatedBy = userId;
            lead.CreatedDate = DateTime.UtcNow;
            lead.UpdatedDate = null;
            _repository.AddLead(lead);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(id.HasValue ? "Update" : "Create", "Leads", "Success",
            nameof(Lead), lead.Id.ToString(),
            oldValue: oldValue,
            newValue: $"{lead.LeadCode}: {lead.Status}, priority {lead.Priority}, expected value {lead.ExpectedValue}.");
        _logger.LogInformation("Lead {LeadId} saved by {UserId}.", lead.Id, userId);
        return lead;
    }

    public async Task<bool> DeleteLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.FindLeadByIdAsync(id, cancellationToken);
        if (lead is null || !CanAccess(lead.AssignedTo, userId, viewAll))
        {
            return false;
        }

        await _repository.ClearLeadReferencesAsync(id, cancellationToken);
        _repository.RemoveLead(lead);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Delete", "Leads", "Success", nameof(Lead), id.ToString(),
            oldValue: $"{lead.LeadCode}: {lead.Status}.");
        return true;
    }

    public async Task<int?> ConvertLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var lead = await _repository.FindLeadByIdAsync(id, cancellationToken);
        if (lead is null || !CanAccess(lead.AssignedTo, userId, viewAll) ||
            lead.Status is LeadStatus.Converted or LeadStatus.Lost ||
            string.IsNullOrWhiteSpace(lead.Email) ||
            string.IsNullOrWhiteSpace(lead.Name) ||
            await _repository.EmailExistsAsync(lead.Email.Trim(), cancellationToken: cancellationToken) ||
            await _repository.PhoneExistsAsync(lead.Phone.Trim(), cancellationToken: cancellationToken))
        {
            return null;
        }

        var oldStatus = lead.Status;
        var nameParts = lead.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var customer = new Customer
        {
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : "(Unspecified)",
            Email = lead.Email.Trim(),
            Phone = lead.Phone.Trim(),
            Company = lead.Company,
            Status = CustomerStatus.Active,
            AssignedTo = lead.AssignedTo ?? userId,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };
        lead.Status = LeadStatus.Converted;
        lead.ConvertedCustomer = customer;
        lead.UpdatedDate = DateTime.UtcNow;
        _repository.Add(customer);
        _repository.AddActivity(new CustomerActivity
        {
            Customer = customer,
            CustomerName = customer.Name,
            Action = "Converted from lead",
            UserId = userId,
            UserName = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System",
            OccurredAt = DateTime.UtcNow
        });
        _repository.AddCrmActivity(new CrmActivity
        {
            Type = ActivityType.Task,
            Subject = "Lead converted to customer",
            Description = $"Converted from lead {lead.LeadCode}.",
            ActivityDate = DateTime.UtcNow,
            Status = ActivityStatus.Completed,
            Customer = customer,
            AssignedTo = customer.AssignedTo,
            CreatedBy = userId
        });
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Convert", "Leads", "Success", nameof(Lead), id.ToString(),
            oldValue: oldStatus.ToString(), newValue: $"Converted to customer {customer.Id}.");
        return customer.Id;
    }

    public async Task<IReadOnlyList<Opportunity>> GetOpportunitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var opportunities = _repository.Opportunities;
        if (!viewAll)
        {
            opportunities = opportunities.Where(opportunity => opportunity.AssignedTo == null || opportunity.AssignedTo == userId);
        }

        return await opportunities.OrderByDescending(opportunity => opportunity.CreatedDate).ToListAsync(cancellationToken);
    }

    public async Task<Opportunity?> GetOpportunityAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var opportunity = await _repository.FindOpportunityByIdAsync(id, cancellationToken);
        return opportunity is not null && CanAccess(opportunity.AssignedTo, userId, viewAll) ? opportunity : null;
    }

    public async Task<Opportunity?> SaveOpportunityAsync(Opportunity input, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessReferenceAsync(input.CustomerId, input.LeadId, userId, viewAll, cancellationToken))
        {
            return null;
        }

        Opportunity opportunity;
        string? oldValue = null;
        if (id.HasValue)
        {
            var existing = await _repository.FindOpportunityByIdAsync(id.Value, cancellationToken);
            if (existing is null || !CanAccess(existing.AssignedTo, userId, viewAll))
            {
                return null;
            }
            opportunity = existing;
            oldValue = $"{opportunity.Name}: {opportunity.Stage}, amount {opportunity.Amount}, probability {opportunity.Probability}.";
        }
        else
        {
            opportunity = input;
            opportunity.Id = 0;
            opportunity.AssignedTo = userId;
            opportunity.CreatedBy = userId;
            opportunity.CreatedDate = DateTime.UtcNow;
            _repository.AddOpportunity(opportunity);
        }

        opportunity.Name = input.Name.Trim();
        opportunity.CustomerId = input.CustomerId;
        opportunity.LeadId = input.LeadId;
        opportunity.Stage = input.Stage;
        opportunity.Amount = input.Amount;
        opportunity.Probability = input.Probability;
        opportunity.ExpectedCloseDate = input.ExpectedCloseDate;
        opportunity.Source = input.Source.Trim();
        opportunity.Notes = input.Notes.Trim();
        if (id.HasValue)
        {
            opportunity.UpdatedDate = DateTime.UtcNow;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(id.HasValue ? "Update" : "Create", "Opportunities", "Success",
            nameof(Opportunity), opportunity.Id.ToString(), oldValue: oldValue,
            newValue: $"{opportunity.Name}: {opportunity.Stage}, amount {opportunity.Amount}, probability {opportunity.Probability}.");
        return opportunity;
    }

    public async Task<bool> DeleteOpportunityAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var opportunity = await _repository.FindOpportunityByIdAsync(id, cancellationToken);
        if (opportunity is null || !CanAccess(opportunity.AssignedTo, userId, viewAll))
        {
            return false;
        }

        await _repository.ClearOpportunityReferencesAsync(id, cancellationToken);
        _repository.RemoveOpportunity(opportunity);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Delete", "Opportunities", "Success", nameof(Opportunity), id.ToString(),
            oldValue: $"{opportunity.Name}: {opportunity.Stage}.");
        return true;
    }

    public async Task<IReadOnlyList<FollowUp>> GetFollowUpsAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var followUps = _repository.FollowUps;
        if (!viewAll)
        {
            followUps = followUps.Where(followUp => followUp.AssignedTo == null || followUp.AssignedTo == userId);
        }

        return await followUps.OrderBy(followUp => followUp.FollowUpDate).ToListAsync(cancellationToken);
    }

    public async Task<FollowUp?> GetFollowUpAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var followUp = await _repository.FindFollowUpByIdAsync(id, cancellationToken);
        return followUp is not null && CanAccess(followUp.AssignedTo, userId, viewAll) ? followUp : null;
    }

    public async Task<FollowUp?> SaveFollowUpAsync(FollowUp input, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessReferenceAsync(input.CustomerId, input.LeadId, input.OpportunityId, userId, viewAll, cancellationToken))
        {
            return null;
        }

        FollowUp followUp;
        string? oldValue = null;
        var createActivity = input.Status == FollowUpStatus.Completed;
        if (id.HasValue)
        {
            var existing = await _repository.FindFollowUpByIdAsync(id.Value, cancellationToken);
            if (existing is null || !CanAccess(existing.AssignedTo, userId, viewAll))
            {
                return null;
            }
            followUp = existing;
            oldValue = $"{followUp.Subject}: {followUp.Status}, due {followUp.FollowUpDate:O}.";
            createActivity &= existing.Status != FollowUpStatus.Completed;
        }
        else
        {
            followUp = input;
            followUp.Id = 0;
            followUp.AssignedTo = userId;
            followUp.CreatedBy = userId;
            followUp.CreatedDate = DateTime.UtcNow;
            _repository.AddFollowUp(followUp);
        }

        followUp.CustomerId = input.CustomerId;
        followUp.LeadId = input.LeadId;
        followUp.OpportunityId = input.OpportunityId;
        followUp.Subject = input.Subject.Trim();
        followUp.Type = input.Type;
        followUp.Status = input.Status;
        followUp.FollowUpDate = input.FollowUpDate;
        followUp.Notes = input.Notes.Trim();
        if (id.HasValue)
        {
            followUp.UpdatedDate = DateTime.UtcNow;
        }

        if (createActivity)
        {
            _repository.AddCrmActivity(new CrmActivity
            {
                Type = (ActivityType)followUp.Type,
                Subject = followUp.Subject,
                Description = followUp.Notes,
                ActivityDate = DateTime.UtcNow,
                Status = ActivityStatus.Completed,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                AssignedTo = followUp.AssignedTo,
                CreatedBy = userId
            });
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(id.HasValue ? "Update" : "Create", "FollowUps", "Success",
            nameof(FollowUp), followUp.Id.ToString(), oldValue: oldValue,
            newValue: $"{followUp.Subject}: {followUp.Status}, due {followUp.FollowUpDate:O}.");
        return followUp;
    }

    public async Task<bool> DeleteFollowUpAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var followUp = await _repository.FindFollowUpByIdAsync(id, cancellationToken);
        if (followUp is null || !CanAccess(followUp.AssignedTo, userId, viewAll))
        {
            return false;
        }

        _repository.RemoveFollowUp(followUp);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Delete", "FollowUps", "Success", nameof(FollowUp), id.ToString(),
            oldValue: $"{followUp.Subject}: {followUp.Status}.");
        return true;
    }

    public async Task<IReadOnlyList<CrmActivity>> GetActivitiesAsync(
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default)
    {
        var activities = _repository.CrmActivities;
        if (!viewAll)
        {
            activities = activities.Where(activity => activity.AssignedTo == null || activity.AssignedTo == userId);
        }

        return await activities.OrderByDescending(activity => activity.ActivityDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<CrmActivity?> CreateActivityAsync(
        CrmActivity activity,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessReferenceAsync(activity.CustomerId, activity.LeadId, activity.OpportunityId,
                userId, viewAll, cancellationToken))
        {
            return null;
        }

        activity.Id = 0;
        activity.AssignedTo = userId;
        activity.CreatedBy = userId;
        _repository.AddCrmActivity(activity);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Create", "Activities", "Success", nameof(CrmActivity),
            activity.Id.ToString(), newValue: $"{activity.Type}: {activity.Subject}.");
        return activity;
    }

    private static bool CanAccess(string? ownerId, string? userId, bool viewAll) =>
        viewAll || ownerId is null || ownerId == userId;

    private async Task<bool> CanAccessReferenceAsync(
        int? customerId,
        int? leadId,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken) =>
        (customerId is null || viewAll ||
         await _repository.Customers.AnyAsync(customer =>
             customer.Id == customerId && (customer.AssignedTo == null || customer.AssignedTo == userId), cancellationToken)) &&
        (leadId is null || viewAll ||
         await _repository.Leads.AnyAsync(lead =>
             lead.Id == leadId && (lead.AssignedTo == null || lead.AssignedTo == userId), cancellationToken));

    private async Task<bool> CanAccessReferenceAsync(
        int? customerId,
        int? leadId,
        int? opportunityId,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken) =>
        await CanAccessReferenceAsync(customerId, leadId, userId, viewAll, cancellationToken) &&
        (opportunityId is null || viewAll ||
         await _repository.Opportunities.AnyAsync(opportunity =>
             opportunity.Id == opportunityId && (opportunity.AssignedTo == null || opportunity.AssignedTo == userId), cancellationToken));

}
