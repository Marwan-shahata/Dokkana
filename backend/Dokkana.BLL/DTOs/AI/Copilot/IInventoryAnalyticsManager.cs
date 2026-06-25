using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public interface IInventoryAnalyticsManager
{
    Task<List<LowStockProductDto>> GetLowStockProductsAsync(
        int storeId,
        int threshold,
        int limit,
        CancellationToken cancellationToken = default);
}