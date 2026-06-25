using Dokkana.BLL.DTOs.Product;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dokkana.BLL.Managers.Products;

public class ProductManager : IProductManager
{
    private readonly AppDbContext _context;

    // Shopping filler words (English / Arabic / Franco-Arabic) that carry no
    // product meaning. Removed before keyword matching so they don't match
    // everything (e.g. "a" appears in almost every product title).
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "want", "need", "looking", "for", "the", "a", "an", "me", "please",
        "show", "find", "get", "buy", "some", "any", "my", "to", "of", "in",
        "on", "with", "and", "or", "is", "are", "do", "you", "have", "basic",
        "ana", "3ayz", "3ayza", "3awz", "3awza", "3ayez", "aiz", "aiza", "3aaz",
        "عايز", "عايزة", "عاوز", "عاوزة", "محتاج", "محتاجة", "بدور", "ابحث",
        "اريد", "عن", "على", "من", "لو", "سمحت", "ممكن", "يا", "في", "ال"
    };

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
            .Include(p => p.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var allTokens = filter.SearchText
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

            // Drop filler/stop words ("i want a ...", "عايز", "3ayz") and 1-char
            // tokens so a meaningless word like "a" doesn't match every product.
            var terms = allTokens
                .Where(t => t.Length > 1 && !StopWords.Contains(t))
                .ToList();

            // If the query was only filler, fall back to the raw tokens.
            if (terms.Count == 0)
                terms = allTokens;

            query = query.Where(BuildSearchPredicate(terms));
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

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var productRows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return productRows.Select(p => new ProductDto
        {
            Id = p.Id,
            Title = p.Title,
            Price = ActiveSale(p) ? p.SalePrice!.Value : p.Price,
            OriginalPrice = ActiveSale(p) ? p.OriginalPrice ?? p.Price : null,
            SalePrice = ActiveSale(p) ? p.SalePrice : null,
            IsOnSale = ActiveSale(p),
            Color = p.Color,
            Size = p.Size,
            StoreName = p.Store?.Name,
            ImageUrl = p.ImageUrl,
            StoreId = p.StoreId,
            StoreAddress = p.Store?.Address,
            StoreLatitude = p.Store?.Latitude,
            StoreLongitude = p.Store?.Longitude,
            Rating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
            ReviewCount = p.Reviews.Count
        }).ToList();
    }

    private static Expression<Func<Product, bool>> BuildSearchPredicate(IEnumerable<string> terms)
    {
        Expression<Func<Product, bool>>? predicate = null;

        foreach (var term in terms)
        {
            var keyword = term;
            Expression<Func<Product, bool>> termPredicate = p =>
                p.Title.ToLower().Contains(keyword) ||
                (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                (p.Color != null && p.Color.ToLower().Contains(keyword)) ||
                (p.Size != null && p.Size.ToLower().Contains(keyword)) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(keyword));

            predicate = predicate is null
                ? termPredicate
                : Or(predicate, termPredicate);
        }

        return predicate ?? (_ => true);
    }

    private static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = left.Parameters[0];
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)
            ?? throw new InvalidOperationException("Could not compose product search predicate.");

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left.Body, rightBody),
            parameter);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _from ? _to : base.VisitParameter(node);
        }
    }

    private static bool ActiveSale(Product p)
    {
        var now = DateTime.UtcNow;
        return p.SalePrice.HasValue &&
               (!p.SaleStartsAt.HasValue || p.SaleStartsAt.Value <= now) &&
               (!p.SaleEndsAt.HasValue || p.SaleEndsAt.Value >= now);
    }
}
