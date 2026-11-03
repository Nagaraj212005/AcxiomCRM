using AcxiomCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "AdministratorOnly")]
public sealed class AuditController : Controller
{
    private readonly ICustomerRepository _repository;

    public AuditController(ICustomerRepository repository) => _repository = repository;

    public async Task<IActionResult> Index(
        string? user,
        string? module,
        string? action,
        DateTime? from,
        DateTime? to,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 25;
        var logs = _repository.AuditLogs;
        if (!string.IsNullOrWhiteSpace(user))
        {
            logs = logs.Where(log => log.UserName.Contains(user.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(module))
        {
            logs = logs.Where(log => log.Module == module);
        }
        if (!string.IsNullOrWhiteSpace(action))
        {
            logs = logs.Where(log => log.Action == action);
        }
        if (from.HasValue)
        {
            var start = from.Value.Date;
            logs = logs.Where(log => log.CreatedDate >= start);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            logs = logs.Where(log => log.CreatedDate < end);
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize), 1);
        page = Math.Clamp(page, 1, totalPages);
        ViewData["Page"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;
        ViewData["UserFilter"] = user;
        ViewData["ModuleFilter"] = module;
        ViewData["ActionFilter"] = action;
        ViewData["FromFilter"] = from?.ToString("yyyy-MM-dd");
        ViewData["ToFilter"] = to?.ToString("yyyy-MM-dd");
        ViewData["Modules"] = await _repository.AuditLogs.Select(log => log.Module).Distinct().OrderBy(item => item).ToListAsync(cancellationToken);
        ViewData["Actions"] = await _repository.AuditLogs.Select(log => log.Action).Distinct().OrderBy(item => item).ToListAsync(cancellationToken);

        return View(await logs.OrderByDescending(log => log.CreatedDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken));
    }
}
