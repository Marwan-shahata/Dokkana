using Dokkana.BLL.DTOs.AI.Catalog;

namespace Dokkana.BLL.AI.Catalog;

public interface ICatalogRetrievalService
{
    Task<List<RetrievedCatalogChunkDto>> RetrieveAsync(
        int storeId,
        string question,
        int maxChunks,
        CancellationToken cancellationToken = default);
}