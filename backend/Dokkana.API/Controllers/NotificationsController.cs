using System.Security.Claims;
using Dokkana.BLL.DTOs.Notification;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationManager _notificationManager;

        public NotificationsController(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Current user's notifications, newest first.
        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var notifications = await _notificationManager.GetForUserAsync(UserId);
            return Ok(GeneralResult<IEnumerable<NotificationDTO>>.Ok(notifications));
        }

        // Badge count for the bell icon.
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await _notificationManager.GetUnreadCountAsync(UserId);
            return Ok(GeneralResult<int>.Ok(count));
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var ok = await _notificationManager.MarkAsReadAsync(id, UserId);
            return ok
                ? Ok(GeneralResult.Ok("Notification marked as read."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationManager.MarkAllAsReadAsync(UserId);
            return Ok(GeneralResult.Ok("Notifications marked as read."));
        }

        // Admin: broadcast to all users or a single role.
        [HttpPost("broadcast")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Broadcast(BroadcastNotificationDTO dto)
        {
            var count = await _notificationManager.BroadcastAsync(dto);
            return Ok(GeneralResult<int>.Ok(count, "Notifications sent."));
        }
    }
}
