namespace Dokkana.BLL.DTOs.Product
{
    /// <summary>
    /// Full product details for the detail page - includes description and all attributes.
    /// </summary>
    public class ProductDetailsDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? SaleStartsAt { get; set; }
        public DateTime? SaleEndsAt { get; set; }
        public bool IsOnSale { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? Brand { get; set; }
        public string? Gender { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }
}
