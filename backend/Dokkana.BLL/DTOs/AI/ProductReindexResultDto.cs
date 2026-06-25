namespace Dokkana.BLL.DTOs.AI;

// Result of (re)building semantic embeddings for the product catalog.
public class ProductReindexResultDto
{
    public int ProcessedProducts { get; set; }

    public int UpdatedProducts { get; set; }

    public int FailedProducts { get; set; }

    public string? EmbeddingModel { get; set; }

    public string Message { get; set; } = string.Empty;
}
