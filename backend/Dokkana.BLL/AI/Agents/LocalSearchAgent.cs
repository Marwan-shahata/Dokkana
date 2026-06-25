using Dokkana.BLL.AI.Search;
using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.Product;
using Dokkana.BLL.Managers.Products;

namespace Dokkana.BLL.AI.Agents;

// Hybrid local search: combines a fast keyword query against SQL with a semantic
// (vector / RAG) search over the product catalog. The semantic side only runs
// when an OpenRouter key is configured and products have been embedded; otherwise
// it returns nothing and we fall back to pure keyword search.
public class LocalSearchAgent : ILocalSearchAgent
{
    private readonly IProductManager _productManager;
    private readonly IProductSemanticSearchService _semanticSearch;

    public LocalSearchAgent(
        IProductManager productManager,
        IProductSemanticSearchService semanticSearch)
    {
        _productManager = productManager;
        _semanticSearch = semanticSearch;
    }

    public async Task<List<AiProductResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var filter = new ProductFilterDto
        {
            SearchText = BuildSearchText(intent),
            Category = NormalizeCategory(intent.Category, intent.ProductType),
            Color = intent.Color,
            Size = intent.Size,
            Gender = NormalizeGender(intent.Gender),
            MinPrice = intent.MinPrice,
            MaxPrice = intent.MaxPrice,
            PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber,
            PageSize = pageSize
        };

        var products = await _productManager.GetProductsAsync(
            filter,
            cancellationToken);

        var keywordResults = products.Select(p => new AiProductResultDto
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            OriginalPrice = p.OriginalPrice,
            SalePrice = p.SalePrice,
            IsOnSale = p.IsOnSale,
            Color = p.Color,
            Size = p.Size,
            StoreName = p.StoreName,
            ImageUrl = p.ImageUrl,
            StoreId = p.StoreId,
            StoreAddress = p.StoreAddress,
            StoreLatitude = p.StoreLatitude,
            StoreLongitude = p.StoreLongitude,
            Rating = Math.Round(p.Rating, 1),
            ReviewCount = p.ReviewCount,
            DistanceKm = GeoHelper.DistanceKm(request.UserLat, request.UserLng, p.StoreLatitude, p.StoreLongitude)
        }).ToList();

        // Semantic (RAG) results — empty when no API key / no embeddings.
        var semanticResults = await _semanticSearch.SearchAsync(
            intent, request, pageSize, cancellationToken);

        return Merge(
            ApplyHardPriceConstraints(keywordResults, intent),
            ApplyHardPriceConstraints(semanticResults, intent),
            pageSize);
    }

    // Merge keyword and semantic hits by product id. A product found by both keeps
    // its keyword card but gains the semantic score (so ranking can reward it);
    // semantic-only products are added so meaning-based matches still surface.
    private static List<AiProductResultDto> Merge(
        List<AiProductResultDto> keyword,
        List<AiProductResultDto> semantic,
        int pageSize)
    {
        var byId = new Dictionary<int, AiProductResultDto>();

        foreach (var k in keyword)
            byId[k.Id] = k;

        foreach (var s in semantic)
        {
            if (byId.TryGetValue(s.Id, out var existing))
                existing.SemanticScore = Math.Max(existing.SemanticScore, s.SemanticScore);
            else
                byId[s.Id] = s;
        }

        // Keep keyword matches first, then the strongest semantic-only matches,
        // capped so the UI stays focused.
        return byId.Values
            .OrderByDescending(p => p.SemanticScore > 0 || keyword.Any(k => k.Id == p.Id))
            .ThenByDescending(p => p.SemanticScore)
            .Take(Math.Max(pageSize, 10))
            .ToList();
    }

    private static string BuildSearchText(AiSearchIntentDto intent)
    {
        var terms = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.RawQuery))
            terms.Add(intent.RawQuery);

        if (!string.IsNullOrWhiteSpace(intent.NormalizedArabic))
            terms.Add(intent.NormalizedArabic);

        if (!string.IsNullOrWhiteSpace(intent.NormalizedEnglish))
            terms.Add(intent.NormalizedEnglish);

        if (!string.IsNullOrWhiteSpace(intent.ProductType))
            terms.Add(intent.ProductType);

        if (!string.IsNullOrWhiteSpace(intent.Brand))
            terms.Add(intent.Brand);

        if (intent.SearchTerms != null)
            terms.AddRange(intent.SearchTerms);

        return string.Join(" ", terms
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string? NormalizeCategory(string? category, string? productType)
    {
        var categoryText = category?.Trim().ToLowerInvariant();
        var productText = productType?.Trim().ToLowerInvariant();
        var combined = string.Join(" ", new[] { categoryText, productText }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(combined))
            return null;

        if (ContainsAny(combined, "dress", "gown", "bridal"))
            return "Dresses";

        if (ContainsAny(combined, "shirt", "t-shirt", "tee", "top", "blouse", "polo", "hoodie", "sweater"))
            return "Tops";

        if (ContainsAny(combined, "pant", "pants", "trouser", "trousers", "jean", "jeans", "legging", "leggings"))
            return "Pants";

        if (ContainsAny(combined, "shoe", "shoes", "heel", "heels", "sneaker", "sneakers", "boot", "boots", "sandal", "sandals"))
            return "Shoes";

        if (ContainsAny(combined, "bag", "bags", "purse", "handbag", "crossbody", "tote", "backpack"))
            return "Bags";

        if (ContainsAny(combined, "accessory", "accessories", "belt", "sunglasses", "jewelry", "watch", "scarf"))
            return "Accessories";

        if (ContainsAny(combined, "outerwear", "jacket", "coat", "overcoat", "vest", "blazer"))
            return "Outerwear";

        if (ContainsAny(combined, "activewear", "sportswear", "training", "running", "gym", "performance"))
            return "Activewear";

        return categoryText is "clothing" or "clothes" or "fashion" or "apparel" or "wear" or "garment" or "garments"
            ? null
            : category;
    }

    private static bool ContainsAny(string source, params string[] terms)
    {
        return terms.Any(term => source.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static List<AiProductResultDto> ApplyHardPriceConstraints(
        List<AiProductResultDto> products,
        AiSearchIntentDto intent)
    {
        return products
            .Where(p => !intent.MinPrice.HasValue || p.Price >= intent.MinPrice.Value)
            .Where(p => !intent.MaxPrice.HasValue || p.Price <= intent.MaxPrice.Value)
            .ToList();
    }

    private static string? NormalizeGender(string? gender)
    {
        var value = gender?.Trim().ToLowerInvariant();

        return value switch
        {
            "female" or "woman" or "women" or "lady" or "ladies" or "girl" or "girls" => "Women",
            "male" or "man" or "men" or "gentleman" or "gentlemen" or "boy" or "boys" => "Men",
            "unisex" or "all" or "any" => null,
            _ => gender
        };
    }
}
