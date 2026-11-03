using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public sealed class CustomerReportService : ICustomerReportService
{
    private readonly ICustomerRepository _repository;

    public CustomerReportService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(
        string? search,
        CustomerStatus? status,
        string? city,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default)
    {
        var customers = _repository.Customers;
        if (!viewAll)
        {
            customers = customers.Where(customer => customer.AssignedTo == null || customer.AssignedTo == userId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            customers = customers.Where(customer =>
                customer.FirstName.Contains(term) ||
                customer.LastName.Contains(term) ||
                customer.Company.Contains(term) ||
                customer.Email.Contains(term) ||
                customer.City.Contains(term));
        }

        if (status.HasValue)
        {
            customers = customers.Where(customer => customer.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var selectedCity = city.Trim();
            customers = customers.Where(customer => customer.City == selectedCity);
        }

        return await customers
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var customers = _repository.Customers;
        if (!viewAll)
        {
            customers = customers.Where(customer => customer.AssignedTo == null || customer.AssignedTo == userId);
        }

        return await customers
            .Where(customer => customer.City != string.Empty)
            .Select(customer => customer.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);
    }
}
