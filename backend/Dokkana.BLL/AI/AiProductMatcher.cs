using System.Text.RegularExpressions;
using Dokkana.BLL.DTOs.Product;
using Dokkana.BLL.Managers;

namespace Dokkana.BLL.AI
{
    // Turns a natural-language query into real catalog matches without requiring
    // an LLM: it extracts an optional max price, then ranks products by how many
    // query keywords appear in their title/brand/color/gender/category.
    public interface IAiProductMatcher
    {
        Task<(IReadOnlyList<ProductListDTO> Products, int Total)> MatchAsync(string query, int? storeId, int take = 12);
    }

    public class AiProductMatcher : IAiProductMatcher
    {
        private readonly IProductManager _productManager;

        public AiProductMatcher(IProductManager productManager)
        {
            _productManager = productManager;
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "under", "below", "less", "than", "over",
            "above", "cheap", "cheaper", "egp", "pound", "pounds", "price", "around",
            "show", "find", "want", "need", "looking", "some", "any", "that", "this",
            "تحت", "اقل", "من", "اكتر", "سعر", "جنيه", "عايز", "عاوز", "في",
        };

        public async Task<(IReadOnlyList<ProductListDTO> Products, int Total)> MatchAsync(
            string query, int? storeId, int take = 12)
        {
            var maxPrice = ExtractMaxPrice(query);

            // Pull a generous candidate set (optionally price-bounded) to rank locally.
            var page = await _productManager.GetAllAsync(
                search: null, categoryId: null, minPrice: null, maxPrice: maxPrice,
                color: null, gender: null, page: 1, pageSize: 200);

            var candidates = page.Items.AsEnumerable();
            if (storeId.HasValue)
                candidates = candidates.Where(p => p.StoreId == storeId.Value);

            var tokens = Tokenize(query);

            // Score by keyword overlap across the product's searchable text.
            var scored = candidates
                .Select(p => new { p, score = Score(p, tokens) })
                .ToList();

            var matched = scored.Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.p.Price)
                .Select(x => x.p)
                .ToList();

            // No keyword hit (e.g. only a price was given): fall back to the
            // price-filtered list so the shopper still sees relevant options.
            if (matched.Count == 0)
                matched = scored.Select(x => x.p).OrderBy(p => p.Price).ToList();

            return (matched.Take(take).ToList(), matched.Count);
        }

        private static int Score(ProductListDTO p, IReadOnlyCollection<string> tokens)
        {
            if (tokens.Count == 0) return 0;
            var haystack = string.Join(' ',
                p.Title, p.Brand, p.Color, p.Gender, p.CategoryName).ToLowerInvariant();
            return tokens.Count(t => haystack.Contains(t));
        }

        private static IReadOnlyList<string> Tokenize(string query)
        {
            return Regex.Split(query.ToLowerInvariant(), @"[^\p{L}\p{N}]+")
                .Where(t => t.Length >= 3 && !StopWords.Contains(t) && !t.All(char.IsDigit))
                .Distinct()
                .ToList();
        }

        private static decimal? ExtractMaxPrice(string query)
        {
            // "under 500", "below 300", "less than 250", "< 200", "تحت ٥٠٠"
            var m = Regex.Match(query,
                @"(under|below|less than|cheaper than|max|<|تحت|اقل من)\s*([0-9]+)",
                RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[2].Value, out var price))
                return price;
            return null;
        }
    }
}
