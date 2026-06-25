namespace Dokkana.DAL.Models
{
    // A line in a placed order. Title and unit price are snapshotted so order
    // history stays correct even if the product is later edited or deleted.
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Nullable: the original product may be removed later; the snapshot below
        // keeps the order history readable regardless.
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        // Snapshotted store id for store-owner analytics / Business Copilot, so a
        // sale stays attributed to its store even if the product is later removed.
        public int StoreId { get; set; }

        public string ProductTitle { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
