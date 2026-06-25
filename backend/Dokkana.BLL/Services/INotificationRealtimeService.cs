using Dokkana.BLL.DTOs.Notification;

namespace Dokkana.BLL.Services
{
    // Abstraction over the realtime transport (SignalR). Lives in the BLL so the
    // NotificationManager can push live updates without referencing the API layer;
    // the concrete SignalR implementation is wired up in Dokkana.API.
    public interface INotificationRealtimeService
    {
        Task SendToUserAsync(string userId, NotificationDTO notification);
        Task SendToRoleAsync(string role, NotificationDTO notification);
        Task SendToAllAsync(NotificationDTO notification);
    }
}
