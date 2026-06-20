using Dokkana.BLL.DTOs.Notification;
using Dokkana.BLL.Services;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class NotificationManager : INotificationManager
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRealtimeService _realtimeService;

        public NotificationManager(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationRealtimeService realtimeService)
        {
            _context = context;
            _userManager = userManager;
            _realtimeService = realtimeService;
        }

        public async Task<IEnumerable<NotificationDTO>> GetForUserAsync(string userId)
        {
            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return notifications.Select(Map);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification is null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
                notification.IsRead = true;

            await _context.SaveChangesAsync();
            return notifications.Count > 0;
        }

        public async Task NotifyUserAsync(string userId, string title, string message, string? type = null, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            await _realtimeService.SendToUserAsync(userId, Map(notification));
        }

        public async Task NotifyStoreApprovedAsync(int storeId)
        {
            var store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == storeId);
            if (store?.OwnerId is null) return;

            await NotifyUserAsync(
                store.OwnerId,
                "Store approved",
                $"Your store '{store.Name}' has been approved and is now visible to shoppers.",
                "StoreApproved",
                $"/seller/stores/{store.Id}");
        }

        public async Task NotifyOrderStatusChangedAsync(int orderId, string status)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return;

            await NotifyUserAsync(
                order.UserId,
                "Order status updated",
                $"Your order #{order.Id} status is now {status}.",
                "OrderStatus",
                $"/orders/{order.Id}");
        }

        public async Task<int> BroadcastAsync(BroadcastNotificationDTO dto)
        {
            IReadOnlyList<ApplicationUser> users;

            if (string.IsNullOrWhiteSpace(dto.TargetRole))
            {
                users = await _userManager.Users.ToListAsync();
            }
            else
            {
                users = (await _userManager.GetUsersInRoleAsync(dto.TargetRole)).ToList();
            }

            var notifications = users.Select(u => new Notification
            {
                UserId = u.Id,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type ?? "Broadcast",
                Link = dto.Link
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(dto.TargetRole))
            {
                foreach (var notification in notifications)
                    await _realtimeService.SendToUserAsync(notification.UserId, Map(notification));
            }
            else
            {
                var realtimeNotification = new NotificationDTO
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type ?? "Broadcast",
                    Link = dto.Link,
                    CreatedAt = DateTime.UtcNow
                };

                await _realtimeService.SendToRoleAsync(dto.TargetRole, realtimeNotification);
            }

            return notifications.Count;
        }

        private static NotificationDTO Map(Notification n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            Link = n.Link,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
