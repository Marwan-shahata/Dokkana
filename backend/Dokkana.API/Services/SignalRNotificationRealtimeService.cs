using Dokkana.API.Hubs;
using Dokkana.BLL.DTOs.Notification;
using Dokkana.BLL.Services;
using Microsoft.AspNetCore.SignalR;

namespace Dokkana.API.Services
{
    public class SignalRNotificationRealtimeService : INotificationRealtimeService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationRealtimeService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendToUserAsync(string userId, NotificationDTO notification)
        {
            return _hubContext.Clients
                .Group(NotificationGroups.User(userId))
                .SendAsync("notificationReceived", notification);
        }

        public Task SendToRoleAsync(string role, NotificationDTO notification)
        {
            return _hubContext.Clients
                .Group(NotificationGroups.Role(role))
                .SendAsync("notificationReceived", notification);
        }

        public Task SendToAllAsync(NotificationDTO notification)
        {
            return _hubContext.Clients.All.SendAsync("notificationReceived", notification);
        }
    }
}
