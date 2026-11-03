using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class OpportunitiesController : Controller
{
    private readonly ICrmService _crmService;

    public OpportunitiesController(ICrmService crmService) => _crmService = crmService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _crmService.GetOpportunitiesAsync(CurrentUserId, CanViewAll, cancellationToken));

    public IActionResult Create() => View(new Opportunity());

    [HttpPost]
    public async Task<IActionResult> Create(Opportunity opportunity, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(opportunity);
        }
        var saved = await _crmService.SaveOpportunityAsync(opportunity, null, CurrentUserId, CanViewAll, cancellationToken);
        if (saved is null)
        {
            return Conflict();
        }
        TempData["SuccessMessage"] = "Opportunity created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _crmService.GetOpportunityAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity, CancellationToken cancellationToken)
    {
        if (id != opportunity.Id)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(opportunity);
        }
        if (await _crmService.SaveOpportunityAsync(opportunity, id, CurrentUserId, CanViewAll, cancellationToken) is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _crmService.DeleteOpportunityAsync(id, CurrentUserId, CanViewAll, cancellationToken))
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
