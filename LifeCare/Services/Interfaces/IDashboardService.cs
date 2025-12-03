using LifeCare.ViewModels;

namespace LifeCare.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<HomeDashboardVM> GetHomeDashboardAsync(string userId, DateOnly? date = null);
        Task<DailySummaryVM> GetDailySummaryAsync(string userId, DateOnly date);
    }
}