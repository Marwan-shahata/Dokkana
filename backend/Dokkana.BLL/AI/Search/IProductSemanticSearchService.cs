using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Search;

// Semantic (vector) search over the live product catalog — the "RAG over our
// database" piece. Products are embedded once, then queries are matched by
// meaning (cosine similarity) instead of exact keywords.
public interface IProductSemanticSearchService
{
    // True when an OpenRouter key is configured (embeddings can be generated).
    bool IsConfigured { get; }

    // How many products currently have an embedding stored.
    Task<int> CountIndexedAsync(CancellationToken cancellationToken = default);

    // Build / refresh embeddings for the product catalog.
    Task<ProductReindexResultDto> ReindexAsync(
        bool forceRebuild,
        CancellationToken cancellationToken = default);

    // Return the top-K products most similar in meaning to the query. Returns an
    // empty list (never throws) when embeddings or the API key are unavailable.
    Task<List<AiProductResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        AiSearchRequestDto request,
        int topK,
        CancellationToken cancellationToken = default);

    // Visual search: embed the uploaded image and return the most similar products
    // (the embedding model is multimodal, so images match product text embeddings).
    Task<List<AiProductResultDto>> SearchByImageAsync(
        byte[] imageBytes,
        string contentType,
        AiSearchRequestDto request,
        int topK,
        CancellationToken cancellationToken = default);
}
