using Dokkana.BLL.DTOs.AI.Copilot;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.AI.Copilot;

public class SalesAnalyticsManager : ISalesAnalyticsManager
{
    private readonly AppDbContext _context;

    public SalesAnalyticsManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
        int storeId,
        DateTime from,
        DateTime to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await GetCompletedOrderItems(storeId, from, to)
            .GroupBy(i => new
            {
                i.ProductId,
                i.ProductTitle
            })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductTitle = g.Key.ProductTitle,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(
        int storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var items = GetCompletedOrderItems(storeId, from, to);

        return new SalesSummaryDto
        {
            FromDate = from,
            ToDate = to,
            TotalSaleRecords = await items
                .Select(i => i.OrderId)
                .Distinct()
                .CountAsync(cancellationToken),

            TotalSoldItems = await items
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0,

            TotalRevenue = await items
                .SumAsync(i => (decimal?)(i.UnitPrice * i.Quantity), cancellationToken) ?? 0,

            DifferentProductsSold = await items
                .Select(i => i.ProductId)
                .Distinct()
                .CountAsync(cancellationToken)
        };
    }

    public async Task<List<UnsoldProductDto>> GetUnsoldProductsAsync(
        int storeId,
        DateTime from,
        DateTime to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var soldProductIds = GetCompletedOrderItems(storeId, from, to)
            .Where(i => i.ProductId != null)
            .Select(i => i.ProductId!.Value)
            .Distinct();

        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                p.StoreId == storeId &&
                !soldProductIds.Contains(p.Id))
            .OrderByDescending(p => p.Count)
            .Take(limit)
            .Select(p => new UnsoldProductDto
            {
                ProductId = p.Id,
                ProductTitle = p.Title,
                Count = p.Count,
                Price = p.Price
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<OrderItem> GetCompletedOrderItems(
        int storeId,
        DateTime from,
        DateTime to)
    {
        return _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Completed &&
                o.CreatedAt >= from &&
                o.CreatedAt < to)
            .SelectMany(o => o.Items)
            .Where(i => i.StoreId == storeId);
    }
}
