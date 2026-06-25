using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public class RankingAgent : IRankingAgent
{
    public List<AiRankedResultDto> Rank(
        AiSearchIntentDto intent,
        List<AiProductResultDto> localResults,
        List<AiWebResultDto> webResults)
    {
        var ranked = new List<AiRankedResultDto>();

        foreach (var product in localResults)
        {
            var score = ScoreLocalProduct(intent, product);
            var reason = BuildLocalReason(intent, product);

            product.Score = score;
            product.Reason = reason;

            ranked.Add(new AiRankedResultDto
            {
                Source = "local",
                Title = product.Title,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                SalePrice = product.SalePrice,
                IsOnSale = product.IsOnSale,
                ImageUrl = product.ImageUrl,
                StoreName = product.StoreName,
                Score = score,
                Reason = reason,
                Product = product,
                IsSponsored = product.IsSponsored,
                PromotionId = product.PromotionId
            });
        }

        foreach (var web in webResults)
        {
            var score = ScoreWebResult(intent, web);
            var reason = BuildWebReason(intent, web);

            web.Score = score;
            web.Reason = reason;

            ranked.Add(new AiRankedResultDto
            {
                Source = "web",
                Title = web.Title,
                Description = web.Snippet,
                Url = web.Url,
                Score = score,
                Reason = reason,
                WebResult = web
            });
        }

        return ranked
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    private static double ScoreLocalProduct(
        AiSearchIntentDto intent,
        AiProductResultDto product)
    {
        double score = 60;

        var text = $"{product.Title} {product.Color} {product.Size} {product.StoreName}"
            .ToLowerInvariant();

        AddTermMatches(intent, text, ref score, 8);

        if (!string.IsNullOrWhiteSpace(intent.ProductType) &&
            text.Contains(intent.ProductType.ToLowerInvariant()))
        {
            score += 12;
        }

        if (!string.IsNullOrWhiteSpace(intent.Color) &&
            !string.IsNullOrWhiteSpace(product.Color) &&
            product.Color.Contains(intent.Color, StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
        }

        if (!string.IsNullOrWhiteSpace(intent.Size) &&
            !string.IsNullOrWhiteSpace(product.Size) &&
            product.Size.Contains(intent.Size, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        if (intent.MinPrice.HasValue && product.Price >= intent.MinPrice.Value)
            score += 5;

        if (intent.MaxPrice.HasValue && product.Price <= intent.MaxPrice.Value)
            score += 10;

        // Reward semantic (meaning-based) matches from the vector / RAG search, so
        // a relevant product surfaces even when it shares no exact keywords.
        if (product.SemanticScore > 0)
            score += product.SemanticScore * 40;

        return score;
    }

    private static double ScoreWebResult(
        AiSearchIntentDto intent,
        AiWebResultDto web)
    {
        double score = 35;

        var text = $"{web.Title} {web.Snippet}".ToLowerInvariant();

        AddTermMatches(intent, text, ref score, 5);

        if (!string.IsNullOrWhiteSpace(intent.ProductType) &&
            text.Contains(intent.ProductType.ToLowerInvariant()))
        {
            score += 8;
        }

        if (!string.IsNullOrWhiteSpace(intent.Color) &&
            text.Contains(intent.Color.ToLowerInvariant()))
        {
            score += 6;
        }

        if (ContainsEgyptHint(text))
            score += 5;

        return score;
    }

    private static void AddTermMatches(
        AiSearchIntentDto intent,
        string text,
        ref double score,
        double pointsPerTerm)
    {
        if (intent.SearchTerms == null)
            return;

        foreach (var term in intent.SearchTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;

            if (text.Contains(term.ToLowerInvariant()))
                score += pointsPerTerm;
        }
    }

    private static string BuildLocalReason(
        AiSearchIntentDto intent,
        AiProductResultDto product)
    {
        var reasons = new List<string>();

        if (!string.IsNullOrWhiteSpace(intent.Color) &&
            !string.IsNullOrWhiteSpace(product.Color) &&
            product.Color.Contains(intent.Color, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("مطابق للون المطلوب");
        }

        if (!string.IsNullOrWhiteSpace(intent.Size) &&
            !string.IsNullOrWhiteSpace(product.Size) &&
            product.Size.Contains(intent.Size, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("مطابق للمقاس المطلوب");
        }

        if (intent.MaxPrice.HasValue && product.Price <= intent.MaxPrice.Value)
        {
            reasons.Add("داخل السعر المطلوب");
        }

        if (product.SemanticScore >= 0.5)
        {
            reasons.Add("قريب من طلبك في المعنى");
        }

        if (!string.IsNullOrWhiteSpace(product.StoreName))
        {
            reasons.Add("متوفر في محل محلي");
        }

        return reasons.Any()
            ? string.Join("، ", reasons)
            : "نتيجة محلية قريبة من البحث";
    }

    private static string BuildWebReason(
        AiSearchIntentDto intent,
        AiWebResultDto web)
    {
        var reasons = new List<string>();

        var text = $"{web.Title} {web.Snippet}".ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(intent.ProductType) &&
            text.Contains(intent.ProductType.ToLowerInvariant()))
        {
            reasons.Add("مشابه لنوع المنتج المطلوب");
        }

        if (!string.IsNullOrWhiteSpace(intent.Color) &&
            text.Contains(intent.Color.ToLowerInvariant()))
        {
            reasons.Add("يبدو مرتبطًا باللون المطلوب");
        }

        if (ContainsEgyptHint(text))
        {
            reasons.Add("مرتبط بالسوق المصري أو محلات قريبة");
        }

        return reasons.Any()
            ? string.Join("، ", reasons)
            : "نتيجة من البحث على الإنترنت";
    }

    private static bool ContainsEgyptHint(string text)
    {
        return text.Contains("egypt") ||
               text.Contains("cairo") ||
               text.Contains("القاهرة") ||
               text.Contains("مصر");
    }
}
