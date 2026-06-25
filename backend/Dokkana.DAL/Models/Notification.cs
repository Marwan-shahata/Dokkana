namespace Dokkana.DAL.Models
{
    // A per-user notification (store approved, order status changed, admin broadcast).
    // Persisted so the bell icon can show history; also pushed live over SignalR.
    public class Notification
    {
        public int Id { get; set; }

        // Recipient (ApplicationUser.Id).
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Optional category (e.g. "StoreApproved", "OrderStatus", "Broadcast")
        // and a client-side route the notification links to.
        public string? Type { get; set; }
        public string? Link { get; set; }

        public bool IsRead { get; set; }

        // True for paid "sponsored" pushes bought by a store (shown with a
        // Sponsored label + a popup on the shopper side).
        public bool IsSponsored { get; set; }
        public int? CampaignId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
