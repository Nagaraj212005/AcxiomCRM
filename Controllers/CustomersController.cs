using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        string? search,
        CustomerStatus? status,
        string? city,
        string sortOrder = "name",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = await _customerService.GetPageAsync(
            search, status, city, sortOrder, page, User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            CanViewAllCustomers, cancellationToken);
        return View(model);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    public async Task<IActionResult> Create(Customer customer, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        try
        {
            if (!await _customerService.CreateAsync(customer, User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.Identity?.Name ?? "Unknown", cancellationToken))
            {
                AddDuplicateContactError();
                return View(customer);
            }
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            _logger.LogWarning(exception, "A duplicate customer email or phone was rejected during creation.");
            AddDuplicateContactError();
            return View(customer);
        }

        TempData["SuccessMessage"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value, CurrentUserId, CanViewAllCustomers, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value, CurrentUserId, CanViewAllCustomers, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Customer customer, CancellationToken cancellationToken)
    {
        if (id != customer.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        if (await _customerService.GetByIdAsync(id, CurrentUserId, CanViewAllCustomers, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            if (!await _customerService.UpdateAsync(customer,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.Identity?.Name ?? "Unknown", CanViewAllCustomers, cancellationToken))
            {
                AddDuplicateContactError();
                return View(customer);
            }
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            _logger.LogWarning(exception, "A duplicate customer email or phone was rejected during update.");
            AddDuplicateContactError();
            return View(customer);
        }

        TempData["SuccessMessage"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _customerService.GetByIdAsync(id.Value, CurrentUserId, CanViewAllCustomers, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await _customerService.DeleteAsync(id,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                User.Identity?.Name ?? "Unknown", CanViewAllCustomers, cancellationToken))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Customer deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool CanViewAllCustomers => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);

    private void AddDuplicateContactError() =>
        ModelState.AddModelError(string.Empty, "A customer with this email address or phone number already exists.");

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
