namespace Dokkana.BLL.DTOs.Product
{
    /// <summary>
    /// Lightweight DTO for product listings — no heavy fields.
    /// Includes display names for Category and Store so frontend doesn't need joins.
    /// </summary>
    public class ProductListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? Brand { get; set; }
        public string? Gender { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }
}
