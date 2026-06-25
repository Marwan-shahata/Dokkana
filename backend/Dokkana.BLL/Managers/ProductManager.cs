using Dokkana.BLL.DTOs.Product;
using Dokkana.Common;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class ProductManager : IProductManager
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly INotificationManager _notifications;

        public ProductManager(
            IUnitOfWork unitOfWork,
            AppDbContext context,
            INotificationManager notifications)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _notifications = notifications;
        }

        public async Task<PagedResult<ProductListDTO>> GetAllAsync(
            string? search, int? categoryId, decimal? minPrice, decimal? maxPrice,
            string? color, string? gender, int page, int pageSize)
        {
            var query = _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Store)
                // Only surface products from approved stores in the public catalog.
                .Where(p => p.Store != null && p.Store.IsApproved);

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

        public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
            ProductFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var page = Math.Max(filter.PageNumber, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 50);

            var query = _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Where(p => p.Store != null && p.Store.IsApproved);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var category = filter.Category.ToLower();
                query = query.Where(p =>
                    (p.Category != null && p.Category.Name.ToLower().Contains(category)) ||
                    p.Title.ToLower().Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(filter.Color))
            {
                var productColor = filter.Color.ToLower();
                query = query.Where(p =>
                    (p.Color != null && p.Color.ToLower().Contains(productColor)) ||
                    p.Title.ToLower().Contains(productColor));
            }

            if (!string.IsNullOrWhiteSpace(filter.Size))
            {
                var productSize = filter.Size.ToLower();
                query = query.Where(p =>
                    (p.Size != null && p.Size.ToLower().Contains(productSize)) ||
                    p.Title.ToLower().Contains(productSize));
            }

            if (!string.IsNullOrWhiteSpace(filter.Gender))
            {
                var productGender = filter.Gender.ToLower();
                query = query.Where(p => p.Gender != null && p.Gender.ToLower() == productGender);
            }

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            return await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Color = p.Color,
                    Size = p.Size,
                    StoreName = p.Store != null ? p.Store.Name : null,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync(cancellationToken);
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
            await FireProductAlertsAsync(product, null, 0, isNew: true);

            // Reload with navigation properties for the response.
            return (OwnershipResult.Success, (await GetByIdAsync(product.Id))!);
        }

        public async Task<OwnershipResult> UpdateAsync(int id, ProductUpdateDTO dto, string userId, bool isAdmin)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return OwnershipResult.NotFound;

            if (!await IsOwnedByAsync(product.StoreId, userId, isAdmin))
                return OwnershipResult.Forbidden;

            var oldPrice = product.Price;
            var oldCount = product.Count;

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
            await FireProductAlertsAsync(product, oldPrice, oldCount, isNew: false);
            return OwnershipResult.Success;
        }

        public async Task<OwnershipResult> DeleteAsync(int id, string userId, bool isAdmin)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null) return OwnershipResult.NotFound;

            if (!await IsOwnedByAsync(product.StoreId, userId, isAdmin))
                return OwnershipResult.Forbidden;

            // CartItem -> Product is a Restrict FK, so deleting a product that is
            // still in someone's cart would throw. Remove those cart lines first.
            // (Favorites/reviews cascade, and order lines keep their snapshot via
            // a SetNull product link, so only carts need clearing here.)
            var cartLines = await _unitOfWork.CartItems.FindAsync(ci => ci.ProductId == id);
            foreach (var line in cartLines)
                _unitOfWork.CartItems.Delete(line);

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
            OriginalPrice = ActiveSale(p) ? p.OriginalPrice ?? p.Price : null,
            SalePrice = ActiveSale(p) ? p.SalePrice : null,
            SaleEndsAt = ActiveSale(p) ? p.SaleEndsAt : null,
            IsOnSale = ActiveSale(p),
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
            OriginalPrice = ActiveSale(p) ? p.OriginalPrice ?? p.Price : null,
            SalePrice = ActiveSale(p) ? p.SalePrice : null,
            SaleStartsAt = p.SaleStartsAt,
            SaleEndsAt = p.SaleEndsAt,
            IsOnSale = ActiveSale(p),
            Color = p.Color,
            Size = p.Size,
            Brand = p.Brand,
            Gender = p.Gender,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StoreId = p.StoreId,
            StoreName = p.Store?.Name ?? string.Empty
        };

        private static bool ActiveSale(Product p)
        {
            var now = DateTime.UtcNow;
            return p.SalePrice.HasValue &&
                   (!p.SaleStartsAt.HasValue || p.SaleStartsAt.Value <= now) &&
                   (!p.SaleEndsAt.HasValue || p.SaleEndsAt.Value >= now);
        }

        private async Task FireProductAlertsAsync(Product product, decimal? oldPrice, int oldCount, bool isNew)
        {
            var queryText = $"{product.Title} {product.Description} {product.Color} {product.Size} {product.Brand}".ToLowerInvariant();
            var alerts = await _context.ShopperAlerts
                .Where(a => a.IsActive)
                .ToListAsync();

            foreach (var alert in alerts)
            {
                var match = alert.Type switch
                {
                    ShopperAlertType.BackInStock => alert.ProductId == product.Id && oldCount <= 0 && product.Count > 0,
                    ShopperAlertType.PriceDrop => alert.ProductId == product.Id &&
                                                  oldPrice.HasValue &&
                                                  product.Price < oldPrice.Value &&
                                                  (!alert.TargetPrice.HasValue || product.Price <= alert.TargetPrice.Value),
                    ShopperAlertType.NewMatch => isNew &&
                                                 !string.IsNullOrWhiteSpace(alert.Query) &&
                                                 queryText.Contains(alert.Query.Trim().ToLowerInvariant()),
                    _ => false
                };

                if (!match) continue;

                alert.LastTriggeredAt = DateTime.UtcNow;
                await _notifications.NotifyUserAsync(
                    alert.UserId,
                    alert.Type == ShopperAlertType.PriceDrop ? "Price drop" : "Product alert",
                    $"{product.Title} matches your saved alert.",
                    "Alert",
                    $"/products/{product.Id}");
            }

            if (alerts.Any(a => a.LastTriggeredAt.HasValue))
                await _context.SaveChangesAsync();
        }
    }
}
