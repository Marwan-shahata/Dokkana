using Dokkana.BLL.DTOs.Dashboard;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    // Read-only aggregate stats for the admin and seller dashboards.
    public class DashboardManager : IDashboardManager
    {
        private readonly AppDbContext _context;

        public DashboardManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatsDTO> GetAdminStatsAsync()
        {
            return new AdminStatsDTO
            {
                UsersCount = await _context.Users.CountAsync(),
                StoresCount = await _context.Stores.CountAsync(),
                PendingStoresCount = await _context.Stores.CountAsync(s => !s.IsApproved),
                ProductsCount = await _context.Products.CountAsync(),
                OrdersCount = await _context.Orders.CountAsync(),
                // Revenue is realized money only: count completed orders, not
                // open reservations or cancelled ones.
                Revenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };
        }

        public async Task<SellerStatsDTO?> GetSellerStatsAsync(string ownerId)
        {
            var store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.OwnerId == ownerId);
            if (store is null) return null;

            // This store's order lines are attributed through the snapshotted
            // StoreId, so deleted/renamed products do not change historical stats.
            var storeLines = _context.OrderItems.AsNoTracking()
                .Where(i => i.StoreId == store.Id);

            // Realized sales come from completed orders only.
            var completedLines = storeLines.Where(i => i.Order!.Status == OrderStatus.Completed);

            return new SellerStatsDTO
            {
                StoreId = store.Id,
                StoreName = store.Name,
                IsApproved = store.IsApproved,
                ProductsCount = await _context.Products.CountAsync(p => p.StoreId == store.Id),
                // Active orders received (cancelled ones excluded).
                OrdersCount = await storeLines
                    .Where(i => i.Order!.Status != OrderStatus.Cancelled)
                    .Select(i => i.OrderId).Distinct().CountAsync(),
                UnitsSold = await completedLines.SumAsync(i => (int?)i.Quantity) ?? 0,
                Revenue = await completedLines.SumAsync(i => (decimal?)(i.UnitPrice * i.Quantity)) ?? 0,
                // Low stock = in stock but running low; out-of-stock is its own metric.
                LowStockProductsCount = await _context.Products.CountAsync(
                    p => p.StoreId == store.Id && p.Count > 0 && p.Count <= 5)
            };
        }
    }
}
