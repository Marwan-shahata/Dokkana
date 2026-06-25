using Dokkana.BLL.AI.Embeddings;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.AI.Catalog;

public class CatalogRetrievalService : ICatalogRetrievalService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;

    public CatalogRetrievalService(
        AppDbContext context,
        IEmbeddingService embeddingService)
    {
        _context = context;
        _embeddingService = embeddingService;
    }

    public async Task<List<RetrievedCatalogChunkDto>> RetrieveAsync(
        int storeId,
        string question,
        int maxChunks,
        CancellationToken cancellationToken = default)
    {
        var take = maxChunks <= 0 ? 5 : maxChunks;

        var rows = await _context.CatalogChunks
            .AsNoTracking()
            .Where(c =>
                c.StoreId == storeId &&
                c.CatalogDocument != null &&
                c.CatalogDocument.Status.ToString() == "Ready")
            .OrderByDescending(c => c.CatalogDocumentId)
            .ThenBy(c => c.ChunkIndex)
            .Select(c => new
            {
                c.Id,
                c.CatalogDocumentId,
                c.ChunkIndex,
                c.Content,
                c.EmbeddingJson
            })
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return new List<RetrievedCatalogChunkDto>();

        var terms = BuildSearchTerms(question);

        double[] questionVector = Array.Empty<double>();

        try
        {
            questionVector = await _embeddingService.EmbedOneAsync(
                question,
                cancellationToken);
        }
        catch
        {
            // لو embedding فشل، هنكمل keyword فقط
            questionVector = Array.Empty<double>();
        }

        var latestCatalogId = rows.Max(x => x.CatalogDocumentId);

        var scored = rows
            .Select(row =>
            {
                var chunkVector = VectorHelper.FromJson(row.EmbeddingJson);

                var vectorScore =
                    questionVector.Length > 0 && chunkVector.Length > 0
                        ? VectorHelper.CosineSimilarity(questionVector, chunkVector)
                        : 0;

                var keywordScore = CalculateKeywordScore(row.Content, terms);

                var latestScore =
                    row.CatalogDocumentId == latestCatalogId
                        ? 1.0
                        : 0.0;

                var finalScore = CalculateFinalScore(
                    vectorScore,
                    keywordScore,
                    latestScore,
                    questionVector.Length > 0);

                return new RetrievedCatalogChunkDto
                {
                    ChunkId = row.Id,
                    CatalogDocumentId = row.CatalogDocumentId,
                    ChunkIndex = row.ChunkIndex,
                    Content = row.Content,
                    VectorScore = Math.Round(vectorScore, 4),
                    KeywordScore = Math.Round(keywordScore, 4),
                    LatestCatalogScore = latestScore,
                    FinalScore = Math.Round(finalScore, 4)
                };
            })
            .Where(x => x.FinalScore > 0)
            .OrderByDescending(x => x.FinalScore)
            .Take(take)
            .ToList();

        // fallback: لو كل scores طلعت صفر، رجّع أول chunks من آخر كتالوج
        if (scored.Count == 0)
        {
            scored = rows
                .Where(x => x.CatalogDocumentId == latestCatalogId)
                .OrderBy(x => x.ChunkIndex)
                .Take(take)
                .Select(x => new RetrievedCatalogChunkDto
                {
                    ChunkId = x.Id,
                    CatalogDocumentId = x.CatalogDocumentId,
                    ChunkIndex = x.ChunkIndex,
                    Content = x.Content,
                    VectorScore = 0,
                    KeywordScore = 0,
                    LatestCatalogScore = 1,
                    FinalScore = 0.01
                })
                .ToList();
        }

        return scored;
    }

    private static double CalculateFinalScore(
        double vectorScore,
        double keywordScore,
        double latestCatalogScore,
        bool hasVector)
    {
        if (hasVector)
        {
            // Hybrid mode
            return
                (vectorScore * 0.70) +
                (keywordScore * 0.25) +
                (latestCatalogScore * 0.05);
        }

        // Keyword fallback mode
        return
            (keywordScore * 0.90) +
            (latestCatalogScore * 0.10);
    }

    private static double CalculateKeywordScore(
        string content,
        List<string> terms)
    {
        if (terms.Count == 0)
            return 0;

        var normalizedContent = NormalizeText(content);

        var matched = 0;

        foreach (var term in terms)
        {
            var normalizedTerm = NormalizeText(term);

            if (normalizedContent.Contains(normalizedTerm))
                matched++;
        }

        return matched == 0
            ? 0
            : (double)matched / terms.Count;
    }

    private static List<string> BuildSearchTerms(string question)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "هل", "في", "عندي", "ايه", "إيه", "ما", "هو", "هي", "من", "عن", "على", "الى", "إلى",
            "بكام", "كام", "سعر", "اسعار", "أسعار",
            "the", "is", "are", "do", "does", "what", "which", "with", "for", "a", "an", "of",
            "price", "available", "have"
        };

        return NormalizeText(question)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1)
            .Where(x => !stopWords.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeText(string text)
    {
        return text
            .ToLowerInvariant()
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ة", "ه")
            .Replace("ى", "ي")
            .Replace("؟", " ")
            .Replace("?", " ")
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace(":", " ")
            .Replace(";", " ")
            .Replace("-", " ")
            .Replace("_", " ");
    }
}