namespace Dokkana.BLL.DTOs.Product;

public sealed class ProductFilterDto
{
    public string? SearchText { get; set; }
    public string? Category { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Gender { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}