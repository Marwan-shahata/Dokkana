namespace Dokkana.BLL.DTOs.AI.Catalog;

public class RetrievedCatalogChunkDto
{
    public int ChunkId { get; set; }

    public int CatalogDocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public double VectorScore { get; set; }

    public double KeywordScore { get; set; }

    public double LatestCatalogScore { get; set; }

    public double FinalScore { get; set; }
}