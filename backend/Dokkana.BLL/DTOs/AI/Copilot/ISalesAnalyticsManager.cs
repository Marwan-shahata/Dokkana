using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public interface ISalesAnalyticsManager
{
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
        int storeId,
        DateTime from,
        DateTime to,
        int limit,
        CancellationToken cancellationToken = default);

    Task<SalesSummaryDto> GetSalesSummaryAsync(
        int storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<List<UnsoldProductDto>> GetUnsoldProductsAsync(
        int storeId,
        DateTime from,
        DateTime to,
        int limit,
        CancellationToken cancellationToken = default);
}