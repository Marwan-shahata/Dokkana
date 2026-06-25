namespace Dokkana.BLL.DTOs.AI.Catalog;

public class CatalogUploadResponseDto
{
    public int CatalogDocumentId { get; set; }

    public int StoreId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ChunksCount { get; set; }

    public string Message { get; set; } = string.Empty;
}