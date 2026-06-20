using Dokkana.BLL.DTOs.Dashboard;
using Dokkana.DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dokkana.DAL.Models;

namespace Dokkana.BLL.Managers
{
    public class DashboardManager : IDashboardManager
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardManager(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                Revenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };
        }

        public async Task<SellerStatsDTO?> GetSellerStatsAsync(string ownerId)
        {
            var store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.OwnerId == ownerId);
            if (store is null) return null;

            var orderItems = _context.OrderItems
                .AsNoTracking()
                .Include(i => i.Product)
                .Where(i => i.Product != null && i.Product.StoreId == store.Id);

            return new SellerStatsDTO
            {
                StoreId = store.Id,
                StoreName = store.Name,
                IsApproved = store.IsApproved,
                ProductsCount = await _context.Products.CountAsync(p => p.StoreId == store.Id),
                OrdersCount = await orderItems.Select(i => i.OrderId).Distinct().CountAsync(),
                UnitsSold = await orderItems.SumAsync(i => (int?)i.Quantity) ?? 0,
                Revenue = await orderItems.SumAsync(i => (decimal?)(i.UnitPrice * i.Quantity)) ?? 0,
                LowStockProductsCount = await _context.Products.CountAsync(p => p.StoreId == store.Id && p.Count <= 5)
            };
        }

        public async Task<IEnumerable<UserSummaryDTO>> GetUsersAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var result = new List<UserSummaryDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserSummaryDTO
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Role = roles.FirstOrDefault() ?? "Shopper"
                });
            }

            return result;
        }
    }
}
