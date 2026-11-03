using AcxiomCRM.Models;

namespace AcxiomCRM.Interfaces;

public interface ICrmService
{
    Task<IReadOnlyList<Lead>> GetLeadsAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<Lead?> GetLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<Lead?> SaveLeadAsync(Lead lead, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<bool> DeleteLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<int?> ConvertLeadAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Opportunity>> GetOpportunitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<Opportunity?> GetOpportunityAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<Opportunity?> SaveOpportunityAsync(Opportunity opportunity, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<bool> DeleteOpportunityAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FollowUp>> GetFollowUpsAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<FollowUp?> GetFollowUpAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<FollowUp?> SaveFollowUpAsync(FollowUp followUp, int? id, string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<bool> DeleteFollowUpAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmActivity>> GetActivitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default);
    Task<CrmActivity?> CreateActivityAsync(CrmActivity activity, string? userId, bool viewAll, CancellationToken cancellationToken = default);
}
