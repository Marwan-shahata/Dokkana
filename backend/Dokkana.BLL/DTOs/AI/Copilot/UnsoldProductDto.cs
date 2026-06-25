namespace Dokkana.BLL.DTOs.AI.Copilot;

public class UnsoldProductDto
{
    public int? ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Price { get; set; }
}
