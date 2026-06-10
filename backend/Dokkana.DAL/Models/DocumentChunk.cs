namespace Dokkana.DAL.Models
{
    // One embedded chunk of a catalog PDF. The Embedding is stored in the vector
    // store (Supabase pgvector) for similarity search during RAG retrieval.
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int CatalogDocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int PageNumber { get; set; }

        // 1536-dim vector from OpenAI text-embedding-3-small. Mapped to pgvector.
        public float[]? Embedding { get; set; }
    }
}
