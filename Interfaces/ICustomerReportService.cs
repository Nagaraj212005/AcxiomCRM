using AcxiomCRM.Models;

namespace AcxiomCRM.Interfaces;

public interface ICustomerReportService
{
    Task<IReadOnlyList<Customer>> GetCustomersAsync(
        string? search,
        CustomerStatus? status,
        string? city,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken = default);
}
