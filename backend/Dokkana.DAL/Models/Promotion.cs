namespace Dokkana.DAL.Models
{
    public enum PromotionType
    {
        // A paid placement that pushes one product to the top of discovery.
        FeaturedProduct = 0,
        // A paid push notification sent to a shopper audience.
        SponsoredNotification = 1,
        // A paid placement that pushes the store card in store discovery.
        FeaturedStore = 2
    }

    public enum PromotionAudience
    {
        Followers = 0,
        AllShoppers = 1,
        WishlistMatches = 2,
        RecentSearch = 3,
        Nearby = 4
    }

    public enum PromotionStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Active = 3,
        Ended = 4
    }

    // A paid promotion bought by a store. Either a featured product placement or
    // a sponsored push notification to shoppers. Impressions/clicks/recipients
    // drive the merchant's campaign stats (M4).
    public class Promotion
    {
        public int Id { get; set; }

        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public PromotionType Type { get; set; }

        // FeaturedProduct only.
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        // SponsoredNotification only.
        public string? Title { get; set; }
        public string? Message { get; set; }
        public PromotionAudience Audience { get; set; }

        public decimal Budget { get; set; }
        public PromotionStatus Status { get; set; } = PromotionStatus.Pending;
        public string? RejectionReason { get; set; }

        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public int RecipientsCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
