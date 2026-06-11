using Dokkana.BLL.DTOs.Product;
using Dokkana.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers.Products;

public class ProductManager : IProductManager
{
    private readonly AppDbContext _context;

    public ProductManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        ProductFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var cat = filter.Category.ToLower();
            query = query.Where(p =>
                (p.Category != null && p.Category.Name.ToLower().Contains(cat)) ||
                p.Title.ToLower().Contains(cat));
        }

        if (!string.IsNullOrWhiteSpace(filter.Color))
        {
            var col = filter.Color.ToLower();
            query = query.Where(p =>
                (p.Color != null && p.Color.ToLower().Contains(col)) ||
                p.Title.ToLower().Contains(col));
        }

        if (!string.IsNullOrWhiteSpace(filter.Size))
        {
            var sz = filter.Size.ToLower();
            query = query.Where(p =>
                (p.Size != null && p.Size.ToLower().Contains(sz)) ||
                p.Title.ToLower().Contains(sz));
        }

        if (!string.IsNullOrWhiteSpace(filter.Gender))
        {
            var g = filter.Gender.ToLower();
            query = query.Where(p => p.Gender != null && p.Gender.ToLower() == g);
        }

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        var products = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
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

        return products;
    }
}
