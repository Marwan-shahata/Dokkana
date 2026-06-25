namespace Dokkana.DAL.Models
{
    public enum ShopperAlertType
    {
        BackInStock = 0,
        PriceDrop = 1,
        NewMatch = 2
    }

    // Shopper demand signal: "tell me when this product comes back, drops in
    // price, or when a query finally exists." Also feeds paid campaign targeting.
    public class ShopperAlert
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        public ShopperAlertType Type { get; set; }
        public string? Query { get; set; }
        public decimal? TargetPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggeredAt { get; set; }
    }
}
