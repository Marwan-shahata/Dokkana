using Dokkana.BLL.DTOs.Cart;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class CartManager : ICartManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDTO> GetCartAsync(string userId)
        {
            var cart = await LoadCartAsync(userId);
            return cart is null ? Empty() : Map(cart);
        }

        public async Task<GeneralResult<CartDTO>> AddItemAsync(string userId, AddCartItemDTO dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product is null)
                return GeneralResult<CartDTO>.Fail("Product not found.");

            // Products from stores that are not yet approved must not be orderable.
            var store = await _unitOfWork.Stores.GetByIdAsync(product.StoreId);
            if (store is null || !store.IsApproved)
                return GeneralResult<CartDTO>.Fail("This product is not available.");

            var cart = await LoadCartAsync(userId) ?? await CreateCartAsync(userId);

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
            var newQty = (existing?.Quantity ?? 0) + dto.Quantity;
            if (newQty > product.Count)
                return GeneralResult<CartDTO>.Fail($"Only {product.Count} in stock.");

            if (existing is not null)
            {
                existing.Quantity = newQty;
                _unitOfWork.CartItems.Update(DetachCartItem(existing));
            }
            else
            {
                await _unitOfWork.CartItems.AddAsync(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return GeneralResult<CartDTO>.Ok(Map((await LoadCartAsync(userId))!));
        }

        public async Task<GeneralResult<CartDTO>> UpdateItemAsync(string userId, int productId, UpdateCartItemDTO dto)
        {
            var cart = await LoadCartAsync(userId);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cart is null || item is null)
                return GeneralResult<CartDTO>.Fail("Item not in cart.");

            if (item.Product is null)
                return GeneralResult<CartDTO>.Fail("Product not found.");

            if (dto.Quantity > item.Product.Count)
                return GeneralResult<CartDTO>.Fail($"Only {item.Product.Count} in stock.");

            item.Quantity = dto.Quantity;
            _unitOfWork.CartItems.Update(DetachCartItem(item));
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult<CartDTO>.Ok(Map((await LoadCartAsync(userId))!));
        }

        public async Task<GeneralResult<CartDTO>> RemoveItemAsync(string userId, int productId)
        {
            var cart = await LoadCartAsync(userId);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cart is null || item is null)
                return GeneralResult<CartDTO>.Fail("Item not in cart.");

            _unitOfWork.CartItems.Delete(DetachCartItem(item));
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult<CartDTO>.Ok(Map((await LoadCartAsync(userId))!));
        }

        public async Task<CartDTO> ClearAsync(string userId)
        {
            var cart = await LoadCartAsync(userId);
            if (cart is null) return Empty();

            foreach (var item in cart.Items.ToList())
                _unitOfWork.CartItems.Delete(DetachCartItem(item));

            await _unitOfWork.SaveChangesAsync();
            return Empty();
        }

        // Helpers

        private Task<Cart?> LoadCartAsync(string userId) =>
            _unitOfWork.Carts.Query()
                .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Store)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        private async Task<Cart> CreateCartAsync(string userId)
        {
            var cart = new Cart { UserId = userId };
            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            return cart;
        }

        private static CartDTO Empty() => new();

        private static CartItem DetachCartItem(CartItem item)
        {
            item.Cart = null;
            item.Product = null;
            return item;
        }

        private static CartDTO Map(Cart cart)
        {
            var items = cart.Items
                .Where(i => i.Product is not null)
                .Select(i => new CartItemDTO
                {
                    ProductId = i.ProductId,
                    Title = i.Product!.Title,
                    ImageUrl = i.Product.ImageUrl,
                    StoreName = i.Product.Store?.Name ?? string.Empty,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity,
                    AvailableStock = i.Product.Count
                })
                .ToList();

            return new CartDTO
            {
                Items = items,
                TotalItems = items.Sum(i => i.Quantity),
                TotalAmount = items.Sum(i => i.LineTotal)
            };
        }
    }
}
