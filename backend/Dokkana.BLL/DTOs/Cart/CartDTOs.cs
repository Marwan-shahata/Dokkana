using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Cart
{
    // A single line in the shopper's cart, enriched with live product data.
    public class CartItemDTO
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CartDTO
    {
        public IReadOnlyList<CartItemDTO> Items { get; set; } = new List<CartItemDTO>();
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class AddCartItemDTO
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDTO
    {
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
    }
}
