using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class ActivitiesController : Controller
{
    private readonly ICrmService _crmService;

    public ActivitiesController(ICrmService crmService) => _crmService = crmService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _crmService.GetActivitiesAsync(CurrentUserId, CanViewAll, cancellationToken));

    public IActionResult Create() => View(new CrmActivity());

    [HttpPost]
    public async Task<IActionResult> Create(CrmActivity activity, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(activity);
        }

        if (await _crmService.CreateActivityAsync(activity, CurrentUserId, CanViewAll, cancellationToken) is null)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Activity recorded.";
        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
