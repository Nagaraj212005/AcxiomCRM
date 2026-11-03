namespace AcxiomCRM.Models;

public sealed class CustomerListViewModel
{
    public IReadOnlyList<Customer> Customers { get; init; } = [];

    public string? Search { get; init; }

    public CustomerStatus? Status { get; init; }

    public string? City { get; init; }

    public IReadOnlyList<string> Cities { get; init; } = [];

    public string SortOrder { get; init; } = "name";

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalCount { get; init; }
}
