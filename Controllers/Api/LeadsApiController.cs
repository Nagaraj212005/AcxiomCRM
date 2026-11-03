using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

/// <summary>Provides authenticated lead management and conversion endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CrmAccess")]
public sealed class LeadsController : ControllerBase
{
    private readonly ICrmService _crmService;

    public LeadsController(ICrmService crmService) => _crmService = crmService;

    /// <summary>Lists leads visible to the signed-in user.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The visible leads, newest first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Lead>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<Lead>> Get(CancellationToken cancellationToken) =>
        _crmService.GetLeadsAsync(CurrentUserId, CanViewAll, cancellationToken);

    /// <summary>Returns a lead by identifier.</summary>
    /// <param name="id">Lead identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The lead, or <c>404</c> if unavailable.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Lead>> Get(int id, CancellationToken cancellationToken)
    {
        var lead = await _crmService.GetLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return lead is null ? NotFound() : Ok(lead);
    }

    /// <summary>Creates a lead and generates its lead code.</summary>
    /// <param name="lead">Validated lead data. The server assigns owner and lead code.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The created lead.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Lead>> Post(Lead lead, CancellationToken cancellationToken)
    {
        var saved = await _crmService.SaveLeadAsync(lead, null, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? Conflict() : CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    /// <summary>Updates a lead that is not already converted.</summary>
    /// <param name="id">Lead identifier; it must match the body identifier.</param>
    /// <param name="lead">Updated lead values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated lead.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Lead), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Lead>> Put(int id, Lead lead, CancellationToken cancellationToken)
    {
        if (id != lead.Id)
        {
            return BadRequest();
        }

        var saved = await _crmService.SaveLeadAsync(lead, id, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? NotFound() : Ok(saved);
    }

    /// <summary>Deletes a lead and its dependent follow-ups.</summary>
    /// <param name="id">Lead identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204</c> on success or <c>404</c> if unavailable.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await _crmService.DeleteLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Converts a lead to a customer if its contact information is valid and unique.</summary>
    /// <param name="id">Lead identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The created customer identifier and resource location.</returns>
    [HttpPost("{id:int}/convert")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Convert(int id, CancellationToken cancellationToken)
    {
        var customerId = await _crmService.ConvertLeadAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return customerId is null ? Conflict(new { message = "Lead cannot be converted; check its status and contact details." })
            : Created($"/api/customers/{customerId}", new { customerId });
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
