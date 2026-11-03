using AcxiomCRM.Models;

namespace AcxiomCRM.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(
        string? userId,
        bool viewAll,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}
