using Dokkana.BLL.DTOs.AI.Copilot;
using Dokkana.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.AI.Copilot;

public class InventoryAnalyticsManager : IInventoryAnalyticsManager
{
    private readonly AppDbContext _context;

    public InventoryAnalyticsManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync(
        int storeId,
        int threshold,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                p.StoreId == storeId &&
                p.Count <= threshold)
            .OrderBy(p => p.Count)
            .Take(limit)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                ProductTitle = p.Title,
                Count = p.Count,
                Price = p.Price
            })
            .ToListAsync(cancellationToken);
    }
}