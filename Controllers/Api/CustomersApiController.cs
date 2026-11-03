using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

/// <summary>Provides authenticated customer management endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CrmAccess")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>Returns a page of customers visible to the signed-in user.</summary>
    /// <param name="search">Optional search term for name, company, email, or city.</param>
    /// <param name="status">Optional customer status filter.</param>
    /// <param name="city">Optional city filter.</param>
    /// <param name="sortOrder">Sort key; supported values include name, name_desc, company, email, and created_desc.</param>
    /// <param name="page">One-based page number.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A customer page and paging metadata.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CustomerListViewModel), StatusCodes.Status200OK)]
    public Task<CustomerListViewModel> Get(
        [FromQuery] string? search,
        [FromQuery] CustomerStatus? status,
        [FromQuery] string? city,
        [FromQuery] string sortOrder = "name",
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default) =>
        _customerService.GetPageAsync(search, status, city, sortOrder, page, CurrentUserId, CanViewAll, cancellationToken);

    /// <summary>Returns a customer by identifier.</summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The customer, or <c>404</c> if the customer is unavailable to this user.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Customer>> Get(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>Creates a customer assigned to the signed-in user.</summary>
    /// <param name="customer">Validated customer details.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created customer and its resource location.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Customer>> Post(Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _customerService.CreateAsync(customer, CurrentUserId, User.Identity?.Name ?? "Unknown", cancellationToken))
            {
                return Conflict(new { message = "A customer with this email address or phone number already exists." });
            }
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            _logger.LogWarning(exception, "A duplicate customer email or phone was rejected by the customer API.");
            return Conflict(new { message = "A customer with this email address or phone number already exists." });
        }

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    /// <summary>Updates an existing customer visible to the signed-in user.</summary>
    /// <param name="id">Customer identifier; it must match the request body identifier.</param>
    /// <param name="customer">Updated customer details.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204</c> when updated, <c>400</c> for mismatched identifiers, <c>404</c> if unavailable, or <c>409</c> for duplicate contact details.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(int id, Customer customer, CancellationToken cancellationToken)
    {
        if (id != customer.Id)
        {
            return BadRequest();
        }

        try
        {
            if (!await _customerService.UpdateAsync(customer, CurrentUserId, User.Identity?.Name ?? "Unknown", CanViewAll, cancellationToken))
            {
                var existing = await _customerService.GetByIdAsync(id, CurrentUserId, CanViewAll, cancellationToken);
                return existing is null ? NotFound() :
                    Conflict(new { message = "A customer with this email address or phone number already exists." });
            }
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            _logger.LogWarning(exception, "A duplicate customer email or phone was rejected by the customer API.");
            return Conflict(new { message = "A customer with this email address or phone number already exists." });
        }

        return NoContent();
    }

    /// <summary>Deletes a customer visible to the signed-in user.</summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204</c> on success or <c>404</c> if the customer is unavailable.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await _customerService.DeleteAsync(id, CurrentUserId, User.Identity?.Name ?? "Unknown", CanViewAll, cancellationToken)
            ? NoContent()
            : NotFound();

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
