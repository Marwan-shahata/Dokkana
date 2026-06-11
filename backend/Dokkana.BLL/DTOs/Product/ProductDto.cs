namespace Dokkana.BLL.DTOs.Product;

public sealed class ProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? StoreName { get; set; }
    public string? ImageUrl { get; set; }
}