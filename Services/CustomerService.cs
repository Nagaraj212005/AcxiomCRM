using AcxiomCRM.Interfaces;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public sealed class CustomerService : ICustomerService
{
    private const int PageSize = 10;
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerService> _logger;
    private readonly IAuditService _auditService;

    public CustomerService(ICustomerRepository repository, ILogger<CustomerService> logger, IAuditService auditService)
    {
        _repository = repository;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<CustomerListViewModel> GetPageAsync(
        string? search,
        CustomerStatus? status,
        string? city,
        string sortOrder,
        int page,
        string? userId,
        bool viewAll,
        CancellationToken cancellationToken = default)
    {
        var customers = ApplyFilters(search, status, city, userId, viewAll);
        customers = sortOrder switch
        {
            "name_desc" => customers.OrderByDescending(customer => customer.LastName).ThenByDescending(customer => customer.FirstName),
            "company" => customers.OrderBy(customer => customer.Company).ThenBy(customer => customer.LastName),
            "company_desc" => customers.OrderByDescending(customer => customer.Company).ThenBy(customer => customer.LastName),
            "email" => customers.OrderBy(customer => customer.Email),
            "created" => customers.OrderBy(customer => customer.CreatedDate).ThenBy(customer => customer.Id),
            "created_desc" => customers.OrderByDescending(customer => customer.CreatedDate).ThenByDescending(customer => customer.Id),
            _ => customers.OrderBy(customer => customer.LastName).ThenBy(customer => customer.FirstName)
        };

        var totalCount = await customers.CountAsync(cancellationToken);
        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)PageSize), 1);
        page = Math.Clamp(page, 1, totalPages);

        return new CustomerListViewModel
        {
            Customers = await customers.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken),
            Search = search?.Trim(),
            Status = status,
            City = city?.Trim(),
            SortOrder = sortOrder,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Cities = await GetCitiesAsync(userId, viewAll, cancellationToken)
        };
    }

    public async Task<Customer?> GetByIdAsync(int id, string? userId, bool viewAll, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.FindByIdAsync(id, cancellationToken);
        return customer is not null && CanAccess(customer, userId, viewAll) ? customer : null;
    }

    public async Task<bool> CreateAsync(Customer customer, string? userId, string userName, CancellationToken cancellationToken = default)
    {
        customer.Email = customer.Email.Trim();
        if (await _repository.EmailExistsAsync(customer.Email, cancellationToken: cancellationToken))
        {
            return false;
        }
        if (await _repository.PhoneExistsAsync(customer.Phone.Trim(), cancellationToken: cancellationToken))
        {
            return false;
        }

        customer.Phone = customer.Phone.Trim();
        customer.CreatedDate = DateTime.UtcNow;
        customer.UpdatedDate = null;
        customer.AssignedTo = userId;
        customer.CreatedBy = userId;
        _repository.Add(customer);
        AddActivity("Created", customer, userId, userName);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Create", "Customers", "Success", nameof(Customer), customer.Id.ToString(),
            newValue: $"Created {customer.Name} ({customer.Status}).", cancellationToken: cancellationToken);
        _logger.LogInformation("Customer {CustomerId} created by {UserName}.", customer.Id, userName);
        return true;
    }

    public async Task<bool> UpdateAsync(Customer customer, string? userId, string userName, bool viewAll, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.FindByIdAsync(customer.Id, cancellationToken);
        if (existing is null || !CanAccess(existing, userId, viewAll))
        {
            return false;
        }

        var email = customer.Email.Trim();
        var phone = customer.Phone.Trim();
        if (await _repository.EmailExistsAsync(email, customer.Id, cancellationToken))
        {
            return false;
        }
        if (await _repository.PhoneExistsAsync(phone, customer.Id, cancellationToken))
        {
            return false;
        }

        var oldSummary = $"Customer {existing.Name} ({existing.Status}).";
        existing.FirstName = customer.FirstName.Trim();
        existing.LastName = customer.LastName.Trim();
        existing.Email = email;
        existing.Phone = phone;
        existing.Company = customer.Company.Trim();
        existing.Address = customer.Address.Trim();
        existing.City = customer.City.Trim();
        existing.State = customer.State.Trim();
        existing.Country = customer.Country.Trim();
        existing.Status = customer.Status;
        existing.Notes = customer.Notes.Trim();
        existing.NextFollowUpDate = customer.NextFollowUpDate;
        existing.UpdatedDate = DateTime.UtcNow;

        AddActivity("Updated", existing, userId, userName);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Update", "Customers", "Success", nameof(Customer), existing.Id.ToString(),
            oldValue: oldSummary, newValue: $"Updated {existing.Name} ({existing.Status}).",
            cancellationToken: cancellationToken);
        _logger.LogInformation("Customer {CustomerId} updated by {UserName}.", customer.Id, userName);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string? userId, string userName, bool viewAll, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.FindByIdAsync(id, cancellationToken);
        if (customer is null || !CanAccess(customer, userId, viewAll))
        {
            return false;
        }

        _repository.AddActivity(new CustomerActivity
        {
            CustomerId = customer.Id,
            Customer = customer,
            CustomerName = customer.Name,
            Action = "Deleted",
            UserId = userId,
            UserName = userName,
            OccurredAt = DateTime.UtcNow
        });
        await _repository.ClearCustomerReferencesAsync(id, cancellationToken);
        _repository.Remove(customer);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Delete", "Customers", "Success", nameof(Customer), id.ToString(),
            oldValue: $"Deleted customer {customer.Name}.", cancellationToken: cancellationToken);
        _logger.LogInformation("Customer {CustomerId} deleted by {UserName}.", id, userName);
        return true;
    }

    private IQueryable<Customer> ApplyFilters(string? search, CustomerStatus? status, string? city, string? userId, bool viewAll)
    {
        var customers = _repository.Customers;
        if (!viewAll)
        {
            customers = customers.Where(customer => customer.AssignedTo == userId || customer.AssignedTo == null);
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

        return customers;
    }

    private static bool CanAccess(Customer customer, string? userId, bool viewAll) =>
        viewAll || customer.AssignedTo == null || customer.AssignedTo == userId;

    private async Task<IReadOnlyList<string>> GetCitiesAsync(string? userId, bool viewAll, CancellationToken cancellationToken)
    {
        var customers = _repository.Customers;
        if (!viewAll)
        {
            customers = customers.Where(customer => customer.AssignedTo == userId || customer.AssignedTo == null);
        }

        return await customers
            .Where(customer => customer.City != string.Empty)
            .Select(customer => customer.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);
    }

    private void AddActivity(string action, Customer customer, string? userId, string userName) =>
        _repository.AddActivity(new CustomerActivity
        {
            CustomerId = customer.Id,
            Customer = customer,
            CustomerName = customer.Name,
            Action = action,
            UserId = userId,
            UserName = userName,
            OccurredAt = DateTime.UtcNow
        });
}
