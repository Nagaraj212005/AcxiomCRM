using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

/// <summary>Provides authenticated sales opportunity and pipeline endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CrmAccess")]
public sealed class OpportunitiesController : ControllerBase
{
    private readonly ICrmService _crmService;

    public OpportunitiesController(ICrmService crmService) => _crmService = crmService;

    /// <summary>Lists opportunities visible to the signed-in user.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The visible opportunities, newest first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Opportunity>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<Opportunity>> Get(CancellationToken cancellationToken) =>
        _crmService.GetOpportunitiesAsync(CurrentUserId, CanViewAll, cancellationToken);

    /// <summary>Returns an opportunity by identifier.</summary>
    /// <param name="id">Opportunity identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The opportunity, or <c>404</c> if unavailable.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Opportunity>> Get(int id, CancellationToken cancellationToken)
    {
        var item = await _crmService.GetOpportunityAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Creates an opportunity linked to one accessible customer or lead.</summary>
    /// <param name="item">Validated opportunity details.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The new opportunity.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Opportunity>> Post(Opportunity item, CancellationToken cancellationToken)
    {
        var saved = await _crmService.SaveOpportunityAsync(item, null, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? Conflict() : CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    /// <summary>Updates an opportunity visible to the signed-in user.</summary>
    /// <param name="id">Opportunity identifier; it must match the body identifier.</param>
    /// <param name="item">Updated opportunity values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated opportunity.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Opportunity), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Opportunity>> Put(int id, Opportunity item, CancellationToken cancellationToken)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        var saved = await _crmService.SaveOpportunityAsync(item, id, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? NotFound() : Ok(saved);
    }

    /// <summary>Deletes an opportunity and its dependent follow-ups.</summary>
    /// <param name="id">Opportunity identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204</c> on success or <c>404</c> if unavailable.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await _crmService.DeleteOpportunityAsync(id, CurrentUserId, CanViewAll, cancellationToken) ? NoContent() : NotFound();

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
