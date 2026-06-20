namespace Dokkana.BLL.DTOs.Wishlist
{
    public class WishlistItemDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? StoreName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
