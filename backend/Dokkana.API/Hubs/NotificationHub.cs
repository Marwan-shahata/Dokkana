using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dokkana.API.Hubs
{
    // Clients connect here (with their JWT) to receive live "notificationReceived"
    // events. On connect we add them to a per-user group and a group per role so
    // the server can target a single user or a whole role.
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.User(userId));

            foreach (var role in new[] { "Admin", "StoreOwner", "Shopper" })
            {
                if (Context.User?.IsInRole(role) == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.Role(role));
            }

            await base.OnConnectedAsync();
        }
    }

    public static class NotificationGroups
    {
        public static string User(string userId) => $"user:{userId}";
        public static string Role(string role) => $"role:{role}";
    }
}
