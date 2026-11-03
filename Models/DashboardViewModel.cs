namespace AcxiomCRM.Models;

public sealed class DashboardViewModel
{
    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public int CustomerCount { get; init; }

    public int ActiveCustomerCount { get; init; }

    public int NewCustomerCount { get; init; }

    public int PendingFollowUpCount { get; init; }

    public int LeadCount { get; init; }

    public int QualifiedLeadCount { get; init; }

    public int OpenOpportunityCount { get; init; }

    public decimal PipelineValue { get; init; }

    public int ScheduledFollowUpCount { get; init; }

    public IReadOnlyList<Customer> RecentCustomers { get; init; } = [];

    public IReadOnlyList<ChartItemViewModel> CustomersByCity { get; init; } = [];

    public IReadOnlyList<ChartItemViewModel> MonthlyGrowth { get; init; } = [];

    public IReadOnlyList<CustomerActivity> RecentActivities { get; init; } = [];

    public IReadOnlyList<CrmActivity> RecentCrmActivities { get; init; } = [];
}

public sealed class ChartItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public int Value { get; init; }
}
