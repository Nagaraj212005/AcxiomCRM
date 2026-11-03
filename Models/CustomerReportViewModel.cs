namespace AcxiomCRM.Models;

public sealed class CustomerReportViewModel
{
    public IReadOnlyList<Customer> Customers { get; init; } = [];

    public IReadOnlyList<string> Cities { get; init; } = [];

    public string? Search { get; init; }

    public CustomerStatus? Status { get; init; }

    public string? City { get; init; }
}
