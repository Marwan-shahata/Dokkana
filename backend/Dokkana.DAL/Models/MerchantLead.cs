namespace Dokkana.DAL.Models
{
    // Pay-per-lead action captured when a shopper opens WhatsApp or reserves.
    public class MerchantLead
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public Store? Store { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Type { get; set; } = "WhatsApp";
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
