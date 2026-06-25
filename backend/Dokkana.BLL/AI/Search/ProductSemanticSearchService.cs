using Dokkana.BLL.AI.Embeddings;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dokkana.BLL.AI.Search;

public class ProductSemanticSearchService : IProductSemanticSearchService
{
    private const double MinSimilarity = 0.15;

    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly OpenRouterOptions _options;

    public ProductSemanticSearchService(
        AppDbContext db,
        IEmbeddingService embeddingService,
        IOptions<OpenRouterOptions> options)
    {
        _db = db;
        _embeddingService = embeddingService;
        _options = options.Value;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public Task<int> CountIndexedAsync(CancellationToken cancellationToken = default)
        => _db.ProductEmbeddings.CountAsync(e => e.EmbeddingJson != null, cancellationToken);

    public async Task<ProductReindexResultDto> ReindexAsync(
        bool forceRebuild,
        CancellationToken cancellationToken = default)
    {
        var result = new ProductReindexResultDto { EmbeddingModel = _options.EmbeddingModel };

        if (!IsConfigured)
        {
            result.Message = "مفيش OpenRouter API key، فمش هينفع نعمل embeddings. ضيف المفتاح الأول.";
            return result;
        }

        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Store)
            .ToListAsync(cancellationToken);

        var existing = await _db.ProductEmbeddings
            .ToDictionaryAsync(e => e.ProductId, cancellationToken);

        // Decide which products need (re)embedding.
        var toEmbed = new List<(Product Product, string Text, ProductEmbedding Row)>();
        foreach (var product in products)
        {
            var text = BuildProductText(product);
            existing.TryGetValue(product.Id, out var row);

            var needs = forceRebuild
                        || row is null
                        || string.IsNullOrEmpty(row.EmbeddingJson)
                        || row.SourceText != text
                        || row.EmbeddingModel != _options.EmbeddingModel;

            if (!needs)
                continue;

            row ??= new ProductEmbedding { ProductId = product.Id };
            toEmbed.Add((product, text, row));
        }

        result.ProcessedProducts = toEmbed.Count;
        if (toEmbed.Count == 0)
        {
            result.Message = "كل المنتجات لها embeddings محدّثة بالفعل.";
            return result;
        }

        // Embed in batches to stay friendly to the provider's limits.
        const int batchSize = 16;
        for (var i = 0; i < toEmbed.Count; i += batchSize)
        {
            var batch = toEmbed.Skip(i).Take(batchSize).ToList();
            try
            {
                var vectors = await _embeddingService.EmbedManyAsync(
                    batch.Select(b => b.Text).ToList(),
                    cancellationToken);

                for (var j = 0; j < batch.Count; j++)
                {
                    var vector = j < vectors.Count ? vectors[j] : Array.Empty<double>();
                    if (vector.Length == 0)
                    {
                        result.FailedProducts++;
                        continue;
                    }

                    var (_, text, row) = batch[j];
                    row.SourceText = text;
                    row.EmbeddingJson = VectorHelper.ToJson(vector);
                    row.EmbeddingDimensions = vector.Length;
                    row.EmbeddingModel = _options.EmbeddingModel;
                    row.UpdatedAt = DateTime.UtcNow;

                    if (row.Id == 0)
                        _db.ProductEmbeddings.Add(row);

                    result.UpdatedProducts++;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                result.FailedProducts += batch.Count;
            }
        }

        result.Message =
            $"تمت معالجة {result.ProcessedProducts} منتج. تم تحديث {result.UpdatedProducts} وفشل {result.FailedProducts}.";
        return result;
    }

    public async Task<List<AiProductResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        AiSearchRequestDto request,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var empty = new List<AiProductResultDto>();
        if (!IsConfigured)
            return empty;

        var queryText = BuildQueryText(intent);
        if (string.IsNullOrWhiteSpace(queryText))
            return empty;

        double[] queryVector;
        try
        {
            queryVector = await _embeddingService.EmbedOneAsync(queryText, cancellationToken);
        }
        catch
        {
            // No key / provider error: degrade silently to keyword search upstream.
            return empty;
        }

        if (queryVector.Length == 0)
            return empty;

        return await RankByVectorAsync(queryVector, request, topK, "تطابق دلالي (معنى قريب من طلبك)", cancellationToken);
    }

    public async Task<List<AiProductResultDto>> SearchByImageAsync(
        byte[] imageBytes,
        string contentType,
        AiSearchRequestDto request,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var empty = new List<AiProductResultDto>();
        if (!IsConfigured || imageBytes is null || imageBytes.Length == 0)
            return empty;

        double[] queryVector;
        try
        {
            // The image is embedded into the same space as product text embeddings.
            queryVector = await _embeddingService.EmbedImageAsync(imageBytes, contentType, cancellationToken);
        }
        catch
        {
            return empty;
        }

        if (queryVector.Length == 0)
            return empty;

        return await RankByVectorAsync(queryVector, request, topK, "تطابق بصري مع الصورة", cancellationToken);
    }

    // Cosine-matches a query vector (from text OR an image) against the stored
    // product embeddings and maps the hits to result DTOs (with rating + location).
    private async Task<List<AiProductResultDto>> RankByVectorAsync(
        double[] queryVector,
        AiSearchRequestDto request,
        int topK,
        string reason,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ProductEmbeddings
            .AsNoTracking()
            .Where(e => e.EmbeddingJson != null)
            .Include(e => e.Product)!.ThenInclude(p => p!.Store)
            .Include(e => e.Product)!.ThenInclude(p => p!.Reviews)
            .ToListAsync(cancellationToken);

        var scored = new List<AiProductResultDto>();
        foreach (var row in rows)
        {
            var product = row.Product;
            if (product is null)
                continue;

            var vector = VectorHelper.FromJson(row.EmbeddingJson);
            var similarity = VectorHelper.CosineSimilarity(queryVector, vector);
            if (similarity < MinSimilarity)
                continue;

            var rating = product.Reviews.Count > 0
                ? Math.Round(product.Reviews.Average(r => r.Rating), 1)
                : 0;

            scored.Add(new AiProductResultDto
            {
                Id = product.Id,
                Title = product.Title,
                Price = product.Price,
                Color = product.Color,
                Size = product.Size,
                StoreName = product.Store?.Name,
                ImageUrl = product.ImageUrl,
                StoreId = product.StoreId,
                StoreAddress = product.Store?.Address,
                StoreLatitude = product.Store?.Latitude,
                StoreLongitude = product.Store?.Longitude,
                Rating = rating,
                ReviewCount = product.Reviews.Count,
                DistanceKm = GeoHelper.DistanceKm(
                    request.UserLat, request.UserLng,
                    product.Store?.Latitude, product.Store?.Longitude),
                SemanticScore = similarity,
                Score = Math.Round(similarity * 100, 1),
                Reason = reason
            });
        }

        return scored
            .OrderByDescending(p => p.SemanticScore)
            .Take(topK)
            .ToList();
    }

    // Text that represents a product for embedding: title + description + the
    // fashion attributes shoppers describe.
    private static string BuildProductText(Product p)
    {
        var parts = new List<string?>
        {
            p.Title,
            p.Description,
            p.Color is null ? null : $"color {p.Color}",
            p.Size is null ? null : $"size {p.Size}",
            p.Brand is null ? null : $"brand {p.Brand}",
            p.Gender is null ? null : $"for {p.Gender}",
            p.Category?.Name is null ? null : $"category {p.Category!.Name}"
        };

        return string.Join(". ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    // Text to embed for the shopper's query.
    private static string BuildQueryText(AiSearchIntentDto intent)
    {
        var parts = new List<string?>
        {
            intent.RawQuery,
            intent.NormalizedEnglish,
            intent.NormalizedArabic,
            intent.ProductType,
            intent.Color,
            intent.Brand,
            intent.Gender
        };

        if (intent.SearchTerms is { Count: > 0 })
            parts.Add(string.Join(" ", intent.SearchTerms));

        return string.Join(" ", parts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
