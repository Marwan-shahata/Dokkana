namespace Dokkana.DAL.Models;

public class DocumentChunk
{
    public int Id { get; set; }

    public int CatalogDocumentId { get; set; }

    public CatalogDocument? CatalogDocument { get; set; }

    public int StoreId { get; set; }

    public int ChunkIndex { get; set; }

    public int? PageNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? EmbeddingJson { get; set; }

    public int? EmbeddingDimensions { get; set; }

    public string? EmbeddingModel { get; set; }
}