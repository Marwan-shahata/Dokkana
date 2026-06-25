namespace Dokkana.DAL.Models
{
    // A shopper's active cart. One cart per user; items are added before a
    // reservation/order is placed.
    public class Cart
    {
        public int Id { get; set; }

        // Owner of the cart (ApplicationUser.Id).
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
