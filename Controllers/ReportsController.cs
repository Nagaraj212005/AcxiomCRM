using System.Security;
using System.Text;
using System.Security.Claims;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = "CrmAccess")]
public sealed class ReportsController : Controller
{
    private readonly ICustomerReportService _reportService;
    private readonly ICrmService _crmService;
    private readonly ICustomerRepository _repository;

    public ReportsController(
        ICustomerReportService reportService,
        ICrmService crmService,
        ICustomerRepository repository)
    {
        _reportService = reportService;
        _crmService = crmService;
        _repository = repository;
    }

    public async Task<IActionResult> Index(
        string? search,
        CustomerStatus? status,
        string? city,
        CancellationToken cancellationToken)
    {
        return View(new CustomerReportViewModel
        {
            Customers = await _reportService.GetCustomersAsync(search, status, city, CurrentUserId, CanViewAll, cancellationToken),
            Cities = await _reportService.GetCitiesAsync(CurrentUserId, CanViewAll, cancellationToken),
            Search = search?.Trim(),
            Status = status,
            City = city
        });
    }

    public async Task<IActionResult> Csv(
        string? search,
        CustomerStatus? status,
        string? city,
        CancellationToken cancellationToken)
    {
        var customers = await _reportService.GetCustomersAsync(search, status, city, CurrentUserId, CanViewAll, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("First Name,Last Name,Company,Email,Phone,City,State,Country,Status,Created Date,Updated Date,Next Follow-up,Notes");
        foreach (var customer in customers)
        {
            AppendCsvRow(csv,
                customer.FirstName, customer.LastName, customer.Company, customer.Email, customer.Phone,
                customer.City, customer.State, customer.Country, customer.Status.ToString(),
                customer.CreatedDate.ToString("yyyy-MM-dd"),
                customer.UpdatedDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                customer.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                customer.Notes);
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", "customer-report.csv");
    }

    public async Task<IActionResult> Excel(
        string? search,
        CustomerStatus? status,
        string? city,
        CancellationToken cancellationToken)
    {
        var customers = await _reportService.GetCustomersAsync(search, status, city, CurrentUserId, CanViewAll, cancellationToken);
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\"?><Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" ")
            .Append("xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"><Worksheet ss:Name=\"Customers\"><Table>");
        AppendExcelRow(xml, "First Name", "Last Name", "Company", "Email", "Phone", "City", "State", "Country",
            "Status", "Created Date", "Updated Date", "Next Follow-up", "Notes");
        foreach (var customer in customers)
        {
            AppendExcelRow(xml, customer.FirstName, customer.LastName, customer.Company, customer.Email,
                customer.Phone, customer.City, customer.State, customer.Country, customer.Status.ToString(),
                customer.CreatedDate.ToString("yyyy-MM-dd"), customer.UpdatedDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                customer.NextFollowUpDate?.ToString("yyyy-MM-dd") ?? string.Empty, customer.Notes);
        }

        xml.Append("</Table></Worksheet></Workbook>");
        return File(Encoding.UTF8.GetBytes(xml.ToString()), "application/vnd.ms-excel", "customer-report.xls");
    }

    public async Task<IActionResult> Leads(CancellationToken cancellationToken) =>
        View(await _crmService.GetLeadsAsync(CurrentUserId, CanViewAll, cancellationToken));

    public async Task<IActionResult> Opportunities(CancellationToken cancellationToken)
    {
        var opportunities = await _crmService.GetOpportunitiesAsync(CurrentUserId, CanViewAll, cancellationToken);
        ViewData["PipelineTotal"] = opportunities
        .Where(item => item.Stage is not OpportunityStage.Won and not OpportunityStage.Lost)
        .Sum(item => item.Amount);
        ViewData["WeightedTotal"] = opportunities
        .Where(item => item.Stage is not OpportunityStage.Won and not OpportunityStage.Lost)
        .Sum(item => item.WeightedPipeline);
        return View(opportunities);
    }

    public async Task<IActionResult> FollowUps(CancellationToken cancellationToken) =>
        View(await _crmService.GetFollowUpsAsync(CurrentUserId, CanViewAll, cancellationToken));

    public async Task<IActionResult> Activity(CancellationToken cancellationToken)
    {
        var logs = _repository.AuditLogs;
        if (!CanViewAll)
        {
        logs = logs.Where(log => log.UserId == CurrentUserId);
        }

        return View(await logs.OrderByDescending(log => log.CreatedDate).Take(250).ToListAsync(cancellationToken));
    }

    private static void AppendCsvRow(StringBuilder output, params string?[] values) =>
        output.AppendLine(string.Join(",", values.Select(value => EscapeCsv(value ?? string.Empty))));

    private static string EscapeCsv(string value)
    {
        var formulaStart = value.TrimStart();
        if (formulaStart.StartsWith('=') || formulaStart.StartsWith('+') ||
            formulaStart.StartsWith('-') || formulaStart.StartsWith('@'))
        {
            value = $"'{value}";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void AppendExcelRow(StringBuilder output, params string?[] values)
    {
        output.Append("<Row>");
        foreach (var value in values)
        {
            output.Append("<Cell><Data ss:Type=\"String\">")
                .Append(SecurityElement.Escape(value ?? string.Empty))
                .Append("</Data></Cell>");
        }

        output.Append("</Row>");
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanViewAll => User.IsInRole(CrmRoles.Admin) || User.IsInRole(CrmRoles.Manager);
}
