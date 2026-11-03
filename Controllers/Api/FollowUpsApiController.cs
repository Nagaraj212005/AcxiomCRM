using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

/// <summary>Provides authenticated follow-up scheduling and completion endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CrmAccess")]
public sealed class FollowUpsController : ControllerBase
{
    private readonly ICrmService _crmService;

    public FollowUpsController(ICrmService crmService) => _crmService = crmService;

    /// <summary>Lists follow-ups visible to the signed-in user.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The visible follow-ups ordered by due date.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FollowUp>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<FollowUp>> Get(CancellationToken cancellationToken) =>
        _crmService.GetFollowUpsAsync(CurrentUserId, CanViewAll, cancellationToken);

    /// <summary>Returns a follow-up by identifier.</summary>
    /// <param name="id">Follow-up identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The follow-up, or <c>404</c> if unavailable.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FollowUp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUp>> Get(int id, CancellationToken cancellationToken)
    {
        var item = await _crmService.GetFollowUpAsync(id, CurrentUserId, CanViewAll, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Schedules a follow-up for exactly one accessible CRM record.</summary>
    /// <param name="item">Validated follow-up details.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly scheduled follow-up.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FollowUp), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FollowUp>> Post(FollowUp item, CancellationToken cancellationToken)
    {
        var saved = await _crmService.SaveFollowUpAsync(item, null, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? Conflict() : CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    /// <summary>Updates or completes a follow-up.</summary>
    /// <param name="id">Follow-up identifier; it must match the body identifier.</param>
    /// <param name="item">Updated follow-up values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated follow-up.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FollowUp), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUp>> Put(int id, FollowUp item, CancellationToken cancellationToken)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        var saved = await _crmService.SaveFollowUpAsync(item, id, CurrentUserId, CanViewAll, cancellationToken);
        return saved is null ? NotFound() : Ok(saved);
    }

    /// <summary>Deletes a follow-up visible to the signed-in user.</summary>
    /// <param name="id">Follow-up identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204</c> on success or <c>404</c> if unavailable.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await _crmService.DeleteFollowUpAsync(id, CurrentUserId, CanViewAll, cancellationToken) ? NoContent() : NotFound();

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
