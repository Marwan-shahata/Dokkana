namespace Dokkana.BLL.DTOs.AI.Catalog;

public class RebuildCatalogEmbeddingsResponseDto
{
    public int ProcessedChunks { get; set; }

    public int UpdatedChunks { get; set; }

    public int FailedChunks { get; set; }

    public string EmbeddingModel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}