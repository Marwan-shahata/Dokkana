using Dokkana.BLL.DTOs.Notification;

namespace Dokkana.BLL.Managers
{
    public interface INotificationManager
    {
        Task<IEnumerable<NotificationDTO>> GetForUserAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);

        // Domain events.
        Task NotifyUserAsync(
            string userId,
            string title,
            string message,
            string? type = null,
            string? link = null,
            bool isSponsored = false,
            int? campaignId = null);
        Task NotifyStoreApprovedAsync(int storeId);
        Task NotifyOrderStatusChangedAsync(int orderId, string status);

        // Admin: send to all users or to a single role. Returns how many were created.
        Task<int> BroadcastAsync(BroadcastNotificationDTO dto);
    }
}
