using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class LeadsController : Controller
{
    private readonly ICrmService _crmService;

    public LeadsController(ICrmService crmService) => _crmService = crmService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _crmService.GetLeadsAsync(CurrentUserId, CanViewAll, cancellationToken));

    public IActionResult Create() => View(new Lead());

    [HttpPost]
    public async Task<IActionResult> Create(Lead lead, CancellationToken cancellationToken)
    {
        if (lead.Status == LeadStatus.Converted)
        {
            ModelState.AddModelError(nameof(lead.Status), "Use the conversion action to convert a lead.");
        }
        if (!ModelState.IsValid)
        {
            return View(lead);
        }

        var saved = await _crmService.SaveLeadAsync(lead, null, CurrentUserId, CanViewAll, cancellationToken);
        if (saved is null)
        {
            return Conflict();
        }

        TempData["SuccessMessage"] = "Lead created successfully.";
        return RedirectToAction(nameof(Details), new { id = saved.Id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var lead = await _crmService.GetLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return lead is null ? NotFound() : View(lead);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var lead = await _crmService.GetLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return lead is null ? NotFound() : View(lead);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Lead lead, CancellationToken cancellationToken)
    {
        if (id != lead.Id)
        {
            return NotFound();
        }
        if (lead.Status == LeadStatus.Converted)
        {
            var existing = await _crmService.GetLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
            if (existing is not null && existing.Status != LeadStatus.Converted)
            {
                ModelState.AddModelError(nameof(lead.Status), "Use the conversion action to convert a lead.");
            }
        }
        if (!ModelState.IsValid)
        {
            return View(lead);
        }

        if (await _crmService.SaveLeadAsync(lead, id, CurrentUserId, CanViewAll, cancellationToken) is null)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Convert(int id, CancellationToken cancellationToken)
    {
        var customerId = await _crmService.ConvertLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        if (customerId is null)
        {
            TempData["ErrorMessage"] = "Lead could not be converted. Ensure the lead has an email and unique contact details.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Lead converted to customer.";
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _crmService.DeleteLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Lead deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
