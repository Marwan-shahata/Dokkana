namespace Dokkana.BLL.DTOs.Notification
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public bool IsSponsored { get; set; }
        public int? CampaignId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
