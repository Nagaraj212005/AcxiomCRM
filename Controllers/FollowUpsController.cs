using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class FollowUpsController : Controller
{
    private readonly ICrmService _crmService;

    public FollowUpsController(ICrmService crmService) => _crmService = crmService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _crmService.GetFollowUpsAsync(CurrentUserId, CanViewAll, cancellationToken));

    public IActionResult Create() => View(new FollowUp());

    [HttpPost]
    public async Task<IActionResult> Create(FollowUp followUp, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(followUp);
        }
        var saved = await _crmService.SaveFollowUpAsync(followUp, null, CurrentUserId, CanViewAll, cancellationToken);
        if (saved is null)
        {
            return Conflict();
        }
        TempData["SuccessMessage"] = "Follow-up created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _crmService.GetFollowUpAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, FollowUp followUp, CancellationToken cancellationToken)
    {
        if (id != followUp.Id)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(followUp);
        }
        if (await _crmService.SaveFollowUpAsync(followUp, id, CurrentUserId, CanViewAll, cancellationToken) is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _crmService.DeleteFollowUpAsync(id, CurrentUserId, CanViewAll, cancellationToken))
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Follow-up deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
