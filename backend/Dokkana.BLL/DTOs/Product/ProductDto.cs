namespace Dokkana.BLL.DTOs.Product;

public sealed class ProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsOnSale { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? StoreName { get; set; }
    public string? ImageUrl { get; set; }

    // Store location (for the AI shopping assistant to show where to buy).
    public int StoreId { get; set; }
    public string? StoreAddress { get; set; }
    public double? StoreLatitude { get; set; }
    public double? StoreLongitude { get; set; }

    // Average shopper rating (0 if no reviews yet) and how many reviews.
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
}
