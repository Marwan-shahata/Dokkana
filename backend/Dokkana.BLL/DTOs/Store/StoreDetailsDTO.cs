namespace Dokkana.BLL.DTOs.Store
{
    /// <summary>
    /// Full store details including product count and owner info.
    /// </summary>
    public class StoreDetailsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsApproved { get; set; }
        // Commercial plan as a string ("Free" / "Pro") for the frontend.
        public string Tier { get; set; } = "Free";
        public string? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionRenewsAt { get; set; }
        public decimal WalletBalance { get; set; }
        public string? WhatsAppPhone { get; set; }
        public string? OwnerName { get; set; }
        public int ProductCount { get; set; }
    }
}
