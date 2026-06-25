using Dokkana.BLL.DTOs.AI.Catalog;

namespace Dokkana.BLL.AI.Catalog;

public interface ICatalogRagManager
{
    Task<CatalogUploadResponseDto> UploadCatalogAsync(
        int storeId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<CatalogQuestionResponseDto> AskAsync(
        CatalogQuestionRequestDto request,
        CancellationToken cancellationToken = default);
}