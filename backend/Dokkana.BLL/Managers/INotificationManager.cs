using Dokkana.BLL.DTOs.Notification;

namespace Dokkana.BLL.Managers
{
    public interface INotificationManager
    {
        Task<IEnumerable<NotificationDTO>> GetForUserAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task NotifyUserAsync(string userId, string title, string message, string? type = null, string? link = null);
        Task NotifyStoreApprovedAsync(int storeId);
        Task NotifyOrderStatusChangedAsync(int orderId, string status);
        Task<int> BroadcastAsync(BroadcastNotificationDTO dto);
    }
}
