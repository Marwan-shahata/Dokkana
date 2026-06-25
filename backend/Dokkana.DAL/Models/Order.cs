namespace Dokkana.DAL.Models
{
    // A placed reservation/order. The PickupCode is shown to the shopper and
    // presented in-store to collect the items. Acts as the reservation record.
    public class Order
    {
        public int Id { get; set; }

        // Shopper who placed the order (ApplicationUser.Id).
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Reserved;

        // Human-friendly pickup code generated at checkout (e.g. "DKN-7F3A9C").
        public string PickupCode { get; set; } = string.Empty;

        // Snapshot of the order total at checkout time.
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
