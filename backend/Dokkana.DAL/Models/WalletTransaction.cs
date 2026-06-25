namespace Dokkana.DAL.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public Store? Store { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "TopUp";
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
