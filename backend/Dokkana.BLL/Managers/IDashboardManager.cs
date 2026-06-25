using Dokkana.BLL.DTOs.Dashboard;

namespace Dokkana.BLL.Managers
{
    public interface IDashboardManager
    {
        Task<AdminStatsDTO> GetAdminStatsAsync();
        Task<SellerStatsDTO?> GetSellerStatsAsync(string ownerId);
    }
}
