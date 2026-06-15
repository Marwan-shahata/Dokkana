using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class WishlistManager : IWishlistManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public WishlistManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<WishlistItemDTO>> GetAsync(string userId)
        {
            return await _unitOfWork.FavoriteProducts.Query()
                .Include(f => f.Product).ThenInclude(p => p!.Store)
                .Where(f => f.UserId == userId && f.Product != null)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new WishlistItemDTO
                {
                    ProductId = f.ProductId,
                    Title = f.Product!.Title,
                    Price = f.Product.Price,
                    ImageUrl = f.Product.ImageUrl,
                    StoreName = f.Product.Store != null ? f.Product.Store.Name : string.Empty,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<GeneralResult> AddAsync(string userId, int productId)
        {
            var productExists = await _unitOfWork.Products.Query().AnyAsync(p => p.Id == productId);
            if (!productExists) return GeneralResult.Fail("Product not found.");

            var exists = await _unitOfWork.FavoriteProducts.Query()
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);
            if (exists) return GeneralResult.Ok("Product already in wishlist.");

            await _unitOfWork.FavoriteProducts.AddAsync(new FavoriteProduct
            {
                UserId = userId,
                ProductId = productId
            });
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Product added to wishlist.");
        }

        public async Task<GeneralResult> RemoveAsync(string userId, int productId)
        {
            var favorite = await _unitOfWork.FavoriteProducts.Query()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (favorite is null) return GeneralResult.Fail("Wishlist item not found.");

            _unitOfWork.FavoriteProducts.Delete(favorite);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Product removed from wishlist.");
        }
    }
}
