using AcxiomCRM.Models;

namespace AcxiomCRM.Interfaces;

public interface ICustomerService
{
    Task<CustomerListViewModel> GetPageAsync(
        string? search,
        CustomerStatus? status,
        string? city,
        string sortOrder,
        int page,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default);

    Task<bool> CreateAsync(Customer customer, string? userId, string userName, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Customer customer, string? userId, string userName, bool viewAll, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, string? userId, string userName, bool viewAll, CancellationToken cancellationToken = default);
}
