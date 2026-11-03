using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services;

public sealed class AuditService : IAuditService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICustomerRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;

    public AuditService(
        IHttpContextAccessor httpContextAccessor,
        ICustomerRepository repository,
        UserManager<IdentityUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
        _userManager = userManager;
    }

    public async Task RecordAsync(
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
        string? actorUserName = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var principal = context?.User;
        var userId = actorUserId ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = actorUserName ?? principal?.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(userId))
        {
            userName = (await _userManager.FindByIdAsync(userId))?.UserName;
        }

        _repository.AddAudit(new AuditLog
        {
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "Anonymous" : userName,
            Action = action,
            Module = module,
            Result = result,
            EntityName = entityName,
            RecordId = recordId,
            Details = details,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString()
        });

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
