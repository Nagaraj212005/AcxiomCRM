using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ICustomerRepository _repository;

    public DashboardService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(
        string? userId,
        bool viewAll,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var customers = _repository.Customers;
        var leads = _repository.Leads;
        var opportunities = _repository.Opportunities;
        var followUps = _repository.FollowUps;
        var activities = _repository.Activities;
        var crmActivities = _repository.CrmActivities;
        if (!viewAll)
        {
            customers = customers.Where(customer => customer.AssignedTo == null || customer.AssignedTo == userId);
            leads = leads.Where(lead => lead.AssignedTo == null || lead.AssignedTo == userId);
            opportunities = opportunities.Where(opportunity => opportunity.AssignedTo == null || opportunity.AssignedTo == userId);
            followUps = followUps.Where(followUp => followUp.AssignedTo == null || followUp.AssignedTo == userId);
            activities = activities.Where(activity => activity.UserId == userId);
            crmActivities = crmActivities.Where(activity => activity.AssignedTo == null || activity.AssignedTo == userId);
        }
        var now = DateTime.UtcNow;
        var chartEnd = toDate?.Date ?? now.Date;
        var growthStart = new DateTime(chartEnd.Year, chartEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var rangeStart = fromDate?.Date;
        var rangeEnd = toDate?.Date.AddDays(1);
        if (rangeStart.HasValue)
        {
            customers = customers.Where(customer => customer.CreatedDate >= rangeStart.Value);
            leads = leads.Where(lead => lead.CreatedDate >= rangeStart.Value);
            opportunities = opportunities.Where(opportunity => opportunity.CreatedDate >= rangeStart.Value);
            followUps = followUps.Where(followUp => followUp.CreatedDate >= rangeStart.Value);
        }
        if (rangeEnd.HasValue)
        {
            customers = customers.Where(customer => customer.CreatedDate < rangeEnd.Value);
            leads = leads.Where(lead => lead.CreatedDate < rangeEnd.Value);
            opportunities = opportunities.Where(opportunity => opportunity.CreatedDate < rangeEnd.Value);
            followUps = followUps.Where(followUp => followUp.CreatedDate < rangeEnd.Value);
        }
        var chartStart = rangeStart.HasValue && rangeStart.Value > growthStart ? rangeStart.Value : growthStart;
        var chartCustomers = customers.Where(customer => customer.CreatedDate >= chartStart);
        var cityCounts = await customers
            .Where(customer => customer.City != string.Empty)
            .GroupBy(customer => customer.City)
            .Select(group => new ChartItemViewModel { Label = group.Key, Value = group.Count() })
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Label)
            .Take(6)
            .ToListAsync(cancellationToken);

        var monthlyCounts = await chartCustomers
            .Where(customer => customer.CreatedDate >= chartStart)
            .GroupBy(customer => new { customer.CreatedDate.Year, customer.CreatedDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var monthlyGrowth = Enumerable.Range(0, 6)
            .Select(offset => growthStart.AddMonths(offset))
            .Select(month => new ChartItemViewModel
            {
                Label = month.ToString("MMM yy"),
                Value = monthlyCounts
                    .Where(item => item.Year == month.Year && item.Month == month.Month)
                    .Select(item => item.Count)
                    .FirstOrDefault()
            })
            .ToList();

        return new DashboardViewModel
        {
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date,
            CustomerCount = await customers.CountAsync(cancellationToken),
            ActiveCustomerCount = await customers.CountAsync(customer => customer.Status == CustomerStatus.Active, cancellationToken),
            NewCustomerCount = await customers.CountAsync(customer => customer.CreatedDate >= now.AddDays(-30), cancellationToken),
            PendingFollowUpCount = await followUps.CountAsync(
                followUp => followUp.Status == FollowUpStatus.Planned &&
                            followUp.FollowUpDate < now.Date.AddDays(8),
                cancellationToken),
            LeadCount = await leads.CountAsync(cancellationToken),
            QualifiedLeadCount = await leads.CountAsync(lead => lead.Status == LeadStatus.Qualified, cancellationToken),
            OpenOpportunityCount = await opportunities.CountAsync(
                opportunity => opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost,
                cancellationToken),
            PipelineValue = await opportunities
                .Where(opportunity => opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost)
                .SumAsync(opportunity => (decimal?)(opportunity.Amount * opportunity.Probability / 100m), cancellationToken) ?? 0m,
            ScheduledFollowUpCount = await followUps.CountAsync(followUp => followUp.Status == FollowUpStatus.Planned, cancellationToken),
            RecentCustomers = await _repository.Customers
                .Where(customer => (!rangeStart.HasValue || customer.CreatedDate >= rangeStart.Value) &&
                                   (!rangeEnd.HasValue || customer.CreatedDate < rangeEnd.Value) &&
                                   (viewAll || customer.AssignedTo == null || customer.AssignedTo == userId))
                .OrderByDescending(customer => customer.CreatedDate)
                .ThenByDescending(customer => customer.Id)
                .Take(5)
                .ToListAsync(cancellationToken),
            CustomersByCity = cityCounts,
            MonthlyGrowth = monthlyGrowth,
            RecentActivities = await activities
                .OrderByDescending(activity => activity.OccurredAt)
                .Take(8)
                .ToListAsync(cancellationToken),
            RecentCrmActivities = await crmActivities
                .OrderByDescending(activity => activity.ActivityDate)
                .Take(6)
                .ToListAsync(cancellationToken)
        };
    }
}
