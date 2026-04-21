using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    public interface IDashboardService
    {
        // Returns the summary data needed by the internal dashboard page.
        Task<DashboardSummaryViewModel> GetDashboardSummaryAsync();
    }
}
