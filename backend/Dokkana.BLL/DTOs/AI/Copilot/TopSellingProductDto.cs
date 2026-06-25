namespace Dokkana.BLL.DTOs.AI.Copilot;

public class TopSellingProductDto
{
    public int? ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}