namespace AcxiomCRM.Interfaces;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        string module,
        string result,
        string? entityName = null,
        string? recordId = null,
        string? details = null,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default,
        string? actorUserId = null,
        string? actorUserName = null);
}
