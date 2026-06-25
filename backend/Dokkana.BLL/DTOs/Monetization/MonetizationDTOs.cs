namespace Dokkana.BLL.DTOs.Monetization
{
    public class PromotionDTO
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string? ProductTitle { get; set; }
        public decimal? ProductPrice { get; set; }
        public decimal? ProductOriginalPrice { get; set; }
        public bool IsProductOnSale { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string Audience { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public int RecipientsCount { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }

    public class CreateFeaturedPromotionDTO
    {
        public int? ProductId { get; set; }
        public decimal Budget { get; set; } = 100;
        public int DurationDays { get; set; } = 7;
    }

    public class CreateNotificationPromotionDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Audience { get; set; } = "Followers";
        public decimal Budget { get; set; } = 100;
        public int DurationDays { get; set; } = 3;
    }

    public class CampaignDecisionDTO
    {
        public string? Reason { get; set; }
    }

    public class BillingSummaryDTO
    {
        public decimal Balance { get; set; }
        public IReadOnlyList<WalletTransactionDTO> Transactions { get; set; } = [];
    }

    public class TopUpWalletDTO
    {
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    public class WalletTransactionDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLeadDTO
    {
        public int StoreId { get; set; }
        public int? ProductId { get; set; }
        public string Type { get; set; } = "WhatsApp";
    }

    public class MerchantLeadDTO
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string? ProductTitle { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAlertDTO
    {
        public int? ProductId { get; set; }
        public string Type { get; set; } = "BackInStock";
        public string? Query { get; set; }
        public decimal? TargetPrice { get; set; }
    }

    public class AlertDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string? ProductTitle { get; set; }
        public string? Query { get; set; }
        public decimal? TargetPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }

    public class NotificationPreferencesDTO
    {
        public bool SponsoredNotificationsEnabled { get; set; } = true;
    }

    public class TrendingQueryDTO
    {
        public string Query { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AiProductSuggestionRequestDTO
    {
        public string? ImageUrl { get; set; }
        public string? FileName { get; set; }
    }

    public class AiProductSuggestionDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }

    public class ScheduleSaleDTO
    {
        public decimal SalePrice { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
    }
}
