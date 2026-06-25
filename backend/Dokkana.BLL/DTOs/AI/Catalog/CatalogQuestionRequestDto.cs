namespace Dokkana.BLL.DTOs.AI.Catalog;

public class CatalogQuestionRequestDto
{
    public int StoreId { get; set; }

    public string Question { get; set; } = string.Empty;

    public int MaxChunks { get; set; } = 5;
}