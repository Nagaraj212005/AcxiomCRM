using System.Diagnostics;
using AcxiomCRM.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using AcxiomCRM.Models;
using System.Security.Claims;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IDashboardService dashboardService, ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewAll = User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
        {
            ModelState.AddModelError(string.Empty, "The start date must be on or before the end date.");
            from = null;
            to = null;
        }

        return View(await _dashboardService.GetDashboardAsync(userId, viewAll, from, to, HttpContext.RequestAborted));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception is not null)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {Path}.",
                HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path);
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
}
