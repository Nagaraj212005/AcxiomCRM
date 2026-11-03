using AcxiomCRM.Models;
using AcxiomCRM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "AdministratorOnly")]
public sealed class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userList = await _userManager.Users.OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var rolesByUserId = userList.ToDictionary(user => user.Id, _ => new List<string>());
        foreach (var role in CrmRoles.All)
        {
            foreach (var user in await _userManager.GetUsersInRoleAsync(role))
            {
                if (rolesByUserId.TryGetValue(user.Id, out var roles))
                {
                    roles.Add(role);
                }
            }
        }

        var users = new List<UserAdministrationItemViewModel>();
        foreach (var user in userList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            users.Add(new UserAdministrationItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "Unknown",
                Roles = rolesByUserId[user.Id],
                LockoutEnd = user.LockoutEnd,
                IsPermanentlyDisabled = user.LockoutEnd == DateTimeOffset.MaxValue
            });
        }

        return View(new UserAdministrationViewModel
        {
            Users = users,
            Roles = CrmRoles.All
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role) || !CrmRoles.All.Contains(role, StringComparer.Ordinal))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Contains(role, StringComparer.Ordinal))
        {
            TempData["SuccessMessage"] = "User role is unchanged.";
            return RedirectToAction(nameof(Index));
        }
        var oldRoles = string.Join(", ", existingRoles);
        if (existingRoles.Contains(CrmRoles.Admin, StringComparer.Ordinal) &&
            !string.Equals(role, CrmRoles.Admin, StringComparison.Ordinal))
        {
            var admins = await _userManager.GetUsersInRoleAsync(CrmRoles.Admin);
            var enabledAdminCount = admins.Count(admin => admin.LockoutEnd != DateTimeOffset.MaxValue);
            if (enabledAdminCount <= 1 || string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "The last administrator or your own administrator access cannot be removed.";
                return RedirectToAction(nameof(Index));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            _logger.LogError("Unable to assign role {Role} to user {UserId}: {Errors}", role, user.Id,
                string.Join("; ", addResult.Errors.Select(error => error.Description)));
            return Problem("The user's role could not be updated.");
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(
            user, existingRoles.Where(existingRole => !string.Equals(existingRole, role, StringComparison.Ordinal)));
        if (!removeResult.Succeeded)
        {
            _logger.LogError("Unable to remove previous roles from user {UserId}: {Errors}", user.Id,
                string.Join("; ", removeResult.Errors.Select(error => error.Description)));
            return Problem("The user's role could not be updated.");
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            _logger.LogError("Unable to refresh the security stamp for user {UserId}: {Errors}", user.Id,
                string.Join("; ", stampResult.Errors.Select(error => error.Description)));
            return Problem("The user's security state could not be refreshed.");
        }
        await _auditService.RecordAsync("UpdateRole", "Users", "Success", nameof(IdentityUser), user.Id,
            oldValue: oldRoles, newValue: role,
            actorUserId: _userManager.GetUserId(User), actorUserName: User.Identity?.Name);
        TempData["SuccessMessage"] = "User role updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetEnabled(string userId, bool enabled)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }
        if (!enabled && string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "You cannot disable your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (!enabled && await _userManager.IsInRoleAsync(user, CrmRoles.Admin))
        {
            var admins = await _userManager.GetUsersInRoleAsync(CrmRoles.Admin);
            if (admins.Count(admin => admin.LockoutEnd != DateTimeOffset.MaxValue) <= 1)
            {
                TempData["ErrorMessage"] = "The last enabled administrator account cannot be disabled.";
                return RedirectToAction(nameof(Index));
            }
        }

        var wasEnabled = user.LockoutEnd != DateTimeOffset.MaxValue;
        var lockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutEnabledResult.Succeeded)
        {
            _logger.LogError("Unable to enable lockout state for user {UserId}: {Errors}", user.Id,
                string.Join("; ", lockoutEnabledResult.Errors.Select(error => error.Description)));
            return Problem("The user's account access could not be updated.");
        }

        var result = await _userManager.SetLockoutEndDateAsync(
            user,
            enabled ? null : DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            _logger.LogError("Unable to change enabled state for user {UserId}: {Errors}", user.Id,
                string.Join("; ", result.Errors.Select(error => error.Description)));
            return Problem("The account's enabled state could not be updated.");
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            _logger.LogError("Unable to refresh the security stamp for user {UserId}: {Errors}", user.Id,
                string.Join("; ", stampResult.Errors.Select(error => error.Description)));
            return Problem("The user's security state could not be refreshed.");
        }
        await _auditService.RecordAsync(enabled ? "Enable" : "Disable", "Users", "Success",
            nameof(IdentityUser), user.Id, oldValue: wasEnabled ? "Enabled" : "Disabled",
            newValue: enabled ? "Enabled" : "Disabled",
            actorUserId: _userManager.GetUserId(User), actorUserName: User.Identity?.Name);
        TempData["SuccessMessage"] = enabled ? "User account enabled." : "User account disabled.";
        return RedirectToAction(nameof(Index));
    }
}
