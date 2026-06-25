using Dokkana.BLL.DTOs.AI.Catalog;

namespace Dokkana.BLL.AI.Catalog;

public interface ICatalogEmbeddingMaintenanceService
{
    Task<RebuildCatalogEmbeddingsResponseDto> RebuildAsync(
        RebuildCatalogEmbeddingsRequestDto request,
        CancellationToken cancellationToken = default);
}