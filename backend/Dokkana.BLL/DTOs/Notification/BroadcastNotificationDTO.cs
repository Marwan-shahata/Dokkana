using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Notification
{
    public class BroadcastNotificationDTO
    {
        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Optional: Admin, StoreOwner, Shopper. Empty means all users.
        public string? TargetRole { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
    }
}
