using AcxiomCRM.Models;

namespace AcxiomCRM.Interfaces;

public interface ICustomerRepository
{
    IQueryable<Customer> Customers { get; }

    IQueryable<CustomerActivity> Activities { get; }

    IQueryable<Lead> Leads { get; }

    IQueryable<Opportunity> Opportunities { get; }

    IQueryable<FollowUp> FollowUps { get; }

    IQueryable<CrmActivity> CrmActivities { get; }

    IQueryable<AuditLog> AuditLogs { get; }

    IQueryable<Microsoft.AspNetCore.Identity.IdentityUser> Users { get; }

    Task<Customer?> FindByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Lead?> FindLeadByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Opportunity?> FindOpportunityByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<FollowUp?> FindFollowUpByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, int? exceptCustomerId = null, CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(string phone, int? exceptCustomerId = null, CancellationToken cancellationToken = default);

    void Add(Customer customer);

    void AddLead(Lead lead);

    void AddOpportunity(Opportunity opportunity);

    void AddFollowUp(FollowUp followUp);

    void AddCrmActivity(CrmActivity activity);

    void AddActivity(CustomerActivity activity);

    void AddAudit(AuditLog auditLog);

    void Remove(Customer customer);

    void RemoveLead(Lead lead);

    void RemoveOpportunity(Opportunity opportunity);

    void RemoveFollowUp(FollowUp followUp);

    Task ClearCustomerReferencesAsync(int customerId, CancellationToken cancellationToken = default);

    Task ClearLeadReferencesAsync(int leadId, CancellationToken cancellationToken = default);

    Task ClearOpportunityReferencesAsync(int opportunityId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
