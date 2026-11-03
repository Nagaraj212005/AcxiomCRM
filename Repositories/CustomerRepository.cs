using AcxiomCRM.Data;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<Customer> Customers => _context.Customers.AsNoTracking();

    public IQueryable<CustomerActivity> Activities => _context.CustomerActivities.AsNoTracking();

    public IQueryable<Lead> Leads => _context.Leads.AsNoTracking();

    public IQueryable<Opportunity> Opportunities => _context.Opportunities.AsNoTracking();

    public IQueryable<FollowUp> FollowUps => _context.FollowUps.AsNoTracking();

    public IQueryable<CrmActivity> CrmActivities => _context.CrmActivities.AsNoTracking();

    public IQueryable<AuditLog> AuditLogs => _context.AuditLogs.AsNoTracking();

    public IQueryable<Microsoft.AspNetCore.Identity.IdentityUser> Users => _context.Users.AsNoTracking();

    public Task<Customer?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Lead?> FindLeadByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Leads.FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);

    public Task<Opportunity?> FindOpportunityByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Opportunities.FirstOrDefaultAsync(opportunity => opportunity.Id == id, cancellationToken);

    public Task<FollowUp?> FindFollowUpByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.FollowUps.FirstOrDefaultAsync(followUp => followUp.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, int? exceptCustomerId = null, CancellationToken cancellationToken = default) =>
        _context.Customers.AnyAsync(customer =>
            customer.Email == email &&
            (!exceptCustomerId.HasValue || customer.Id != exceptCustomerId.Value), cancellationToken);

    public Task<bool> PhoneExistsAsync(string phone, int? exceptCustomerId = null, CancellationToken cancellationToken = default) =>
        _context.Customers.AnyAsync(customer =>
            customer.Phone == phone &&
            (!exceptCustomerId.HasValue || customer.Id != exceptCustomerId.Value), cancellationToken);

    public void Add(Customer customer) => _context.Customers.Add(customer);

    public void AddLead(Lead lead) => _context.Leads.Add(lead);

    public void AddOpportunity(Opportunity opportunity) => _context.Opportunities.Add(opportunity);

    public void AddFollowUp(FollowUp followUp) => _context.FollowUps.Add(followUp);

    public void AddCrmActivity(CrmActivity activity) => _context.CrmActivities.Add(activity);

    public void AddActivity(CustomerActivity activity) => _context.CustomerActivities.Add(activity);

    public void AddAudit(AuditLog auditLog) => _context.AuditLogs.Add(auditLog);

    public void Remove(Customer customer) => _context.Customers.Remove(customer);

    public void RemoveLead(Lead lead) => _context.Leads.Remove(lead);

    public void RemoveOpportunity(Opportunity opportunity) => _context.Opportunities.Remove(opportunity);

    public void RemoveFollowUp(FollowUp followUp) => _context.FollowUps.Remove(followUp);

    public async Task ClearCustomerReferencesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var leads = await _context.Leads.Where(lead => lead.ConvertedCustomerId == customerId).ToListAsync(cancellationToken);
        foreach (var lead in leads)
        {
            lead.ConvertedCustomerId = null;
        }

        var opportunities = await _context.Opportunities.Where(item => item.CustomerId == customerId).ToListAsync(cancellationToken);
        foreach (var opportunity in opportunities)
        {
            opportunity.CustomerId = null;
        }

        var followUps = await _context.FollowUps.Where(item => item.CustomerId == customerId).ToListAsync(cancellationToken);
        _context.FollowUps.RemoveRange(followUps);

        var activities = await _context.CrmActivities.Where(item => item.CustomerId == customerId).ToListAsync(cancellationToken);
        foreach (var activity in activities)
        {
            activity.CustomerId = null;
        }
    }

    public async Task ClearLeadReferencesAsync(int leadId, CancellationToken cancellationToken = default)
    {
        var opportunities = await _context.Opportunities.Where(item => item.LeadId == leadId).ToListAsync(cancellationToken);
        foreach (var opportunity in opportunities)
        {
            opportunity.LeadId = null;
        }

        var followUps = await _context.FollowUps.Where(item => item.LeadId == leadId).ToListAsync(cancellationToken);
        _context.FollowUps.RemoveRange(followUps);

        var activities = await _context.CrmActivities.Where(item => item.LeadId == leadId).ToListAsync(cancellationToken);
        foreach (var activity in activities)
        {
            activity.LeadId = null;
        }
    }

    public async Task ClearOpportunityReferencesAsync(int opportunityId, CancellationToken cancellationToken = default)
    {
        var followUps = await _context.FollowUps.Where(item => item.OpportunityId == opportunityId).ToListAsync(cancellationToken);
        _context.FollowUps.RemoveRange(followUps);

        var activities = await _context.CrmActivities.Where(item => item.OpportunityId == opportunityId).ToListAsync(cancellationToken);
        foreach (var activity in activities)
        {
            activity.OpportunityId = null;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
