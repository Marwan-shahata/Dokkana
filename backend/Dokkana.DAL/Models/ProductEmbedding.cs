namespace Dokkana.DAL.Models
{
    // Semantic (vector) representation of a product, used for RAG-style search over
    // the live catalog. One row per product; the embedding is stored as JSON text
    // (same approach as the catalog DocumentChunk embeddings). Regenerated whenever
    // the product text changes or the embedding model changes.
    public class ProductEmbedding
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // The text that was embedded (title + description + attributes), kept so we
        // can tell whether a product needs re-embedding.
        public string? SourceText { get; set; }

        // The embedding vector serialized as a JSON array of doubles.
        public string? EmbeddingJson { get; set; }

        public int? EmbeddingDimensions { get; set; }

        public string? EmbeddingModel { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
