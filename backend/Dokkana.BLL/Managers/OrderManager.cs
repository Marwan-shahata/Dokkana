using Dokkana.BLL.DTOs.Order;
using Dokkana.Common;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class OrderManager : IOrderManager
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;

        public OrderManager(IUnitOfWork unitOfWork, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public async Task<GeneralResult<OrderDTO>> CheckoutAsync(string userId)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var cart = await _unitOfWork.Carts.Query()
                    .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Store)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var lines = cart?.Items.Where(i => i.Product is not null).ToList() ?? new List<CartItem>();
                if (cart is null || lines.Count == 0)
                    return GeneralResult<OrderDTO>.Fail("Your cart is empty.");

                // Validate stock for every line before committing anything.
                foreach (var line in lines)
                {
                    if (line.Quantity > line.Product!.Count)
                        return GeneralResult<OrderDTO>.Fail(
                            $"Not enough stock for \"{line.Product.Title}\" (only {line.Product.Count} left).");
                }

                await using var transaction = await _db.Database.BeginTransactionAsync();

                var order = new Order
                {
                    UserId = userId,
                    Status = OrderStatus.Reserved,
                    PickupCode = GeneratePickupCode(),
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var line in lines)
                {
                    var product = line.Product!;

                    // Atomic stock reservation: only decrement if enough stock still
                    // exists at write time. The surrounding transaction rolls back any
                    // earlier line reservations if a later line loses the race.
                    var reserved = await _db.Products
                        .Where(p => p.Id == product.Id && p.Count >= line.Quantity)
                        .ExecuteUpdateAsync(setters =>
                            setters.SetProperty(p => p.Count, p => p.Count - line.Quantity));

                    if (reserved == 0)
                    {
                        await transaction.RollbackAsync();
                        var currentStock = await _db.Products
                            .AsNoTracking()
                            .Where(p => p.Id == product.Id)
                            .Select(p => (int?)p.Count)
                            .FirstOrDefaultAsync();

                        return GeneralResult<OrderDTO>.Fail(
                            currentStock is null
                                ? $"\"{product.Title}\" is no longer available."
                                : $"Not enough stock for \"{product.Title}\" (only {currentStock.Value} left).");
                    }

                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductTitle = product.Title,
                        ImageUrl = product.ImageUrl,
                        // Snapshot the store id so Business Copilot analytics can
                        // attribute the sale even if the product is later deleted.
                        StoreId = product.StoreId,
                        StoreName = product.Store?.Name ?? string.Empty,
                        UnitPrice = product.Price,
                        Quantity = line.Quantity
                    });
                }

                order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

                await _unitOfWork.Orders.AddAsync(order);

                // Empty the cart now that it has become an order.
                foreach (var line in lines)
                    _unitOfWork.CartItems.Delete(line);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return GeneralResult<OrderDTO>.Ok(Map(order), "Reservation confirmed.");
            });
        }

        public async Task<IEnumerable<OrderSummaryDTO>> GetMyOrdersAsync(string userId)
        {
            return await _unitOfWork.Orders.Query()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDTO
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    PickupCode = o.PickupCode,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();
        }

        public async Task<OrderDTO?> GetByIdAsync(string userId, int orderId)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order is null ? null : Map(order);
        }

        public async Task<IEnumerable<OrderAdminSummaryDTO>> GetAllAsync()
        {
            return await _unitOfWork.Orders.Query()
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderAdminSummaryDTO
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    PickupCode = o.PickupCode,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Items.Count,
                    CustomerName = o.User != null ? (o.User.FirstName + " " + o.User.LastName).Trim() : string.Empty,
                    CustomerEmail = o.User != null ? (o.User.Email ?? string.Empty) : string.Empty
                })
                .ToListAsync();
        }

        public async Task<GeneralResult> UpdateStatusAsync(int orderId, string status)
        {
            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
                return GeneralResult.Fail("Invalid status. Use Reserved, Completed, or Cancelled.");

            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return GeneralResult.Fail("Order not found.");

            await ApplyStatusChangeAsync(order, parsed);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Order status updated.");
        }

        public async Task<IEnumerable<StoreOrderDTO>> GetStoreOrdersAsync(string ownerId)
        {
            var store = (await _unitOfWork.Stores.FindAsync(s => s.OwnerId == ownerId)).FirstOrDefault();
            if (store is null) return Enumerable.Empty<StoreOrderDTO>();

            var orders = await _unitOfWork.Orders.Query()
                .Include(o => o.User)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.StoreId == store.Id))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o =>
            {
                // Show only this store's lines (an order may span multiple stores).
                var lines = o.Items
                    .Where(i => i.StoreId == store.Id)
                    .ToList();

                return new StoreOrderDTO
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status.ToString(),
                    PickupCode = o.PickupCode,
                    CustomerName = o.User != null ? (o.User.FirstName + " " + o.User.LastName).Trim() : string.Empty,
                    StoreTotal = lines.Sum(i => i.UnitPrice * i.Quantity),
                    Items = lines.Select(i => new OrderItemDTO
                    {
                        ProductId = i.ProductId,
                        ProductTitle = i.ProductTitle,
                        ImageUrl = i.ImageUrl,
                        StoreName = i.StoreName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList()
                };
            }).ToList();
        }

        public async Task<GeneralResult> UpdateStatusForStoreAsync(int orderId, string status, string ownerId)
        {
            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
                return GeneralResult.Fail("Invalid status. Use Reserved, Completed, or Cancelled.");

            var store = (await _unitOfWork.Stores.FindAsync(s => s.OwnerId == ownerId)).FirstOrDefault();
            if (store is null) return GeneralResult.Fail("You do not have a store.");

            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return GeneralResult.Fail("Order not found.");

            // A store owner may only change orders that include their products.
            var ownsLine = order.Items.Any(i => i.StoreId == store.Id);
            if (!ownsLine) return GeneralResult.Fail("This order does not include your products.");

            await ApplyStatusChangeAsync(order, parsed);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Order status updated.");
        }

        public async Task<int> CancelExpiredReservationsAsync(
            TimeSpan maxReservationAge,
            CancellationToken cancellationToken = default)
        {
            if (maxReservationAge <= TimeSpan.Zero) return 0;

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var cutoff = DateTime.UtcNow.Subtract(maxReservationAge);
                var expiredOrders = await _db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.Status == OrderStatus.Reserved && o.CreatedAt <= cutoff)
                    .OrderBy(o => o.CreatedAt)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (expiredOrders.Count == 0) return 0;

                var cancelled = 0;
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                foreach (var order in expiredOrders)
                {
                    var statusChanged = await _db.Orders
                        .Where(o => o.Id == order.Id && o.Status == OrderStatus.Reserved)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(o => o.Status, OrderStatus.Cancelled),
                            cancellationToken);

                    if (statusChanged == 0) continue;

                    foreach (var item in order.Items.Where(i => i.ProductId is not null))
                    {
                        await _db.Products
                            .Where(p => p.Id == item.ProductId!.Value)
                            .ExecuteUpdateAsync(
                                setters => setters.SetProperty(p => p.Count, p => p.Count + item.Quantity),
                                cancellationToken);
                    }

                    cancelled++;
                }

                await transaction.CommitAsync(cancellationToken);
                return cancelled;
            });
        }

        // Helpers

        // Applies a status transition, restocking on cancel. Does not save -
        // callers commit so ownership/validation can short-circuit first.
        private async Task ApplyStatusChangeAsync(Order order, OrderStatus parsed)
        {
            var previous = order.Status;

            // Cancelling a reservation must return the reserved units to stock.
            // Guard on the state change so re-cancelling can't restock twice.
            if (parsed == OrderStatus.Cancelled && previous != OrderStatus.Cancelled)
            {
                foreach (var item in order.Items.Where(i => i.ProductId != null))
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId!.Value);
                    if (product is null) continue;
                    product.Count += item.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            order.Status = parsed;
            _unitOfWork.Orders.Update(order);
        }


        private static string GeneratePickupCode() =>
            "DKN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        private static OrderDTO Map(Order o) => new()
        {
            Id = o.Id,
            CreatedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            PickupCode = o.PickupCode,
            TotalAmount = o.TotalAmount,
            Items = o.Items.Select(i => new OrderItemDTO
            {
                ProductId = i.ProductId,
                ProductTitle = i.ProductTitle,
                ImageUrl = i.ImageUrl,
                StoreName = i.StoreName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
