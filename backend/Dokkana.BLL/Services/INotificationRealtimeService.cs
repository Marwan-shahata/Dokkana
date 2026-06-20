using Dokkana.BLL.DTOs.Notification;

namespace Dokkana.BLL.Services
{
    public interface INotificationRealtimeService
    {
        Task SendToUserAsync(string userId, NotificationDTO notification);
        Task SendToRoleAsync(string role, NotificationDTO notification);
        Task SendToAllAsync(NotificationDTO notification);
    }
}
