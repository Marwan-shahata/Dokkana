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

        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notifications = await _notificationManager.GetForUserAsync(userId);
            return Ok(GeneralResult<IEnumerable<NotificationDTO>>.Ok(notifications));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _notificationManager.GetUnreadCountAsync(userId);
            return Ok(GeneralResult<int>.Ok(count));
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ok = await _notificationManager.MarkAsReadAsync(id, userId);
            return ok
                ? Ok(GeneralResult.Ok("Notification marked as read."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationManager.MarkAllAsReadAsync(userId);
            return Ok(GeneralResult.Ok("Notifications marked as read."));
        }

        [HttpPost("broadcast")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Broadcast(BroadcastNotificationDTO dto)
        {
            var count = await _notificationManager.BroadcastAsync(dto);
            return Ok(GeneralResult<int>.Ok(count, "Notifications sent."));
        }
    }
}
