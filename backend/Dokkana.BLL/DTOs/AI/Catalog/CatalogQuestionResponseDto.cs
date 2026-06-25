namespace Dokkana.BLL.DTOs.AI.Catalog;

public class CatalogQuestionResponseDto
{
    public int StoreId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string RetrievalMode { get; set; } = string.Empty;
    // hybrid / vector / keyword / fallback

    public List<string> UsedChunks { get; set; } = new();

    public List<RetrievedCatalogChunkDto> RetrievedChunks { get; set; } = new();
}