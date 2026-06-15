using Dokkana.BLL.DTOs.Order;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class OrderManager : IOrderManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GeneralResult<OrderDTO>> CheckoutAsync(string userId)
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
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductTitle = product.Title,
                    ImageUrl = product.ImageUrl,
                    StoreName = product.Store?.Name ?? string.Empty,
                    UnitPrice = product.Price,
                    Quantity = line.Quantity
                });

                // Reserve stock.
                product.Count -= line.Quantity;
                _unitOfWork.Products.Update(product);
            }

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            await _unitOfWork.Orders.AddAsync(order);

            // Empty the cart now that it has become an order.
            foreach (var line in lines)
                _unitOfWork.CartItems.Delete(line);

            // Single SaveChanges => one transaction for order + stock + cart clear.
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult<OrderDTO>.Ok(Map(order), "Reservation confirmed.");
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

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order is null) return GeneralResult.Fail("Order not found.");

            order.Status = parsed;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Order status updated.");
        }

        // Helpers

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
