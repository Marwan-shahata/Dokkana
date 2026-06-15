using Dokkana.BLL.DTOs.Product;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class ProductManager : IProductManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<ProductListDTO>> GetAllAsync(
            string? search, int? categoryId, decimal? minPrice, decimal? maxPrice,
            string? color, string? gender, int page, int pageSize)
        {
            var query = _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Store)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(color))
                query = query.Where(p => p.Color != null && p.Color.Contains(color));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            // Pagination
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToList(p))
                .ToListAsync();

            return PagedResult<ProductListDTO>.Create(items, totalCount, page, pageSize);
        }

        public async Task<ProductDetailsDTO?> GetByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == id);

            return product is null ? null : MapToDetails(product);
        }

        public async Task<PagedResult<ProductListDTO>> GetByStoreAsync(int storeId, int page, int pageSize)
        {
            var query = _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Where(p => p.StoreId == storeId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToList(p))
                .ToListAsync();

            return PagedResult<ProductListDTO>.Create(items, totalCount, page, pageSize);
        }

        public async Task<(OwnershipResult Result, ProductDetailsDTO? Product)> CreateAsync(ProductCreateDTO dto, string ownerId)
        {
            // Security: always bind the product to the caller's own store. The
            // StoreId in the request body is ignored so a StoreOwner cannot
            // create products under another store.
            var store = (await _unitOfWork.Stores.FindAsync(s => s.OwnerId == ownerId)).FirstOrDefault();
            if (store is null) return (OwnershipResult.Forbidden, null);

            var product = new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Count = dto.Count,
                ImageUrl = dto.ImageUrl,
                Color = dto.Color,
                Size = dto.Size,
                Brand = dto.Brand,
                Gender = dto.Gender,
                CategoryId = dto.CategoryId,
                StoreId = store.Id
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Reload with navigation properties for the response.
            return (OwnershipResult.Success, (await GetByIdAsync(product.Id))!);
        }

        public async Task<OwnershipResult> UpdateAsync(int id, ProductUpdateDTO dto, string userId, bool isAdmin)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return OwnershipResult.NotFound;

            if (!await IsOwnedByAsync(product.StoreId, userId, isAdmin))
                return OwnershipResult.Forbidden;

            if (dto.Title is not null) product.Title = dto.Title;
            if (dto.Description is not null) product.Description = dto.Description;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;
            if (dto.Count.HasValue) product.Count = dto.Count.Value;
            if (dto.ImageUrl is not null) product.ImageUrl = dto.ImageUrl;
            if (dto.Color is not null) product.Color = dto.Color;
            if (dto.Size is not null) product.Size = dto.Size;
            if (dto.Brand is not null) product.Brand = dto.Brand;
            if (dto.Gender is not null) product.Gender = dto.Gender;
            if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return OwnershipResult.Success;
        }

        public async Task<OwnershipResult> DeleteAsync(int id, string userId, bool isAdmin)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return OwnershipResult.NotFound;

            if (!await IsOwnedByAsync(product.StoreId, userId, isAdmin))
                return OwnershipResult.Forbidden;

            _unitOfWork.Products.Delete(product);
            await _unitOfWork.SaveChangesAsync();
            return OwnershipResult.Success;
        }

        // True if the user is an Admin or the owner of the given store.
        private async Task<bool> IsOwnedByAsync(int storeId, string userId, bool isAdmin)
        {
            if (isAdmin) return true;
            var store = await _unitOfWork.Stores.GetByIdAsync(storeId);
            return store is not null && store.OwnerId == userId;
        }

        // Mapping

        private static ProductListDTO MapToList(Product p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            Count = p.Count,
            ImageUrl = p.ImageUrl,
            Color = p.Color,
            Size = p.Size,
            Brand = p.Brand,
            Gender = p.Gender,
            CategoryName = p.Category?.Name ?? string.Empty,
            StoreId = p.StoreId,
            StoreName = p.Store?.Name ?? string.Empty
        };

        private static ProductDetailsDTO MapToDetails(Product p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Price = p.Price,
            Count = p.Count,
            ImageUrl = p.ImageUrl,
            Color = p.Color,
            Size = p.Size,
            Brand = p.Brand,
            Gender = p.Gender,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StoreId = p.StoreId,
            StoreName = p.Store?.Name ?? string.Empty
        };
    }
}
