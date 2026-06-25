namespace Dokkana.BLL.DTOs.AI.Catalog;

public class RebuildCatalogEmbeddingsRequestDto
{
    public int? StoreId { get; set; }

    public int? CatalogDocumentId { get; set; }

    public bool ForceRebuild { get; set; } = false;

    public int BatchSize { get; set; } = 16;
}