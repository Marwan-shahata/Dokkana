using System.Text.Json;
using System.Text.RegularExpressions;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public class InputInterpreterAgent : IInputInterpreterAgent
{
    private readonly IOpenRouterClient _openRouterClient;
    private static readonly Regex[] MaxPricePatterns =
    [
        new(@"(?:under|below|less\s+than|max(?:imum)?|up\s+to|budget|cheaper\s+than|<=?|تحت|اقل\s+من|أقل\s+من|لحد|حتى|حدود)\s*(?:egp|جنيه|ج\.م)?\s*(?<price>\d+(?:[,.]\d+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"(?<price>\d+(?:[,.]\d+)?)\s*(?:egp|جنيه|ج\.م)?\s*(?:or\s+less|and\s+under|او\s+اقل|أو\s+اقل|او\s+أقل|أو\s+أقل)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    private static readonly Regex[] MinPricePatterns =
    [
        new(@"(?:over|above|more\s+than|at\s+least|min(?:imum)?|from|>=?|فوق|اكتر\s+من|أكتر\s+من|اكثر\s+من|أكثر\s+من|من)\s*(?:egp|جنيه|ج\.م)?\s*(?<price>\d+(?:[,.]\d+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    public InputInterpreterAgent(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    public async Task<AiSearchIntentDto> InterpretAsync(
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
You are an intent extraction agent for a bilingual Egyptian local fashion marketplace called Dokkana.

The user can write in:
- Arabic
- English
- Franco-Arabic / Arabizi
- Mixed Arabic and English

Examples:
- "عايز تيشيرت اسود"
- "black tshirt under 500"
- "3ayz kotchy abyad"
- "عايز hoodie oversize black"
- "جزمة بني رجالي"
- "2amis azra2"

Important rules:
- Do not reject unknown words.
- Do not depend on fixed categories only.
- Extract soft hints, not strict filters.
- If you are not sure, keep the word inside searchTerms.
- Convert Franco-Arabic to possible Arabic and English meanings when possible.
- Return JSON only.
- Do not write markdown.
- Do not explain.

Return exactly this JSON shape:
{
  "rawQuery": "",
  "normalizedArabic": null,
  "normalizedEnglish": null,
  "searchTerms": [],
  "productType": null,
  "category": null,
  "color": null,
  "size": null,
  "gender": null,
  "brand": null,
  "minPrice": null,
  "maxPrice": null,
  "languageDetected": "arabic|english|franco|mixed"
}
""";

        try
        {
            var content = await _openRouterClient.AskAsync(
                systemPrompt,
                request.Query,
                cancellationToken);

            content = CleanJson(content);

            var intent = JsonSerializer.Deserialize<AiSearchIntentDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (intent == null)
                return BuildFallbackIntent(request.Query);

            intent.RawQuery = request.Query;

            if (intent.SearchTerms == null || intent.SearchTerms.Count == 0)
                intent.SearchTerms = BuildSearchTerms(request.Query);

            ApplyDeterministicPriceHints(intent, request.Query);

            return intent;
        }
        catch
        {
            return BuildFallbackIntent(request.Query);
        }
    }

    private static AiSearchIntentDto BuildFallbackIntent(string query)
    {
        var intent = new AiSearchIntentDto
        {
            RawQuery = query,
            NormalizedArabic = BasicFrancoToArabic(query),
            NormalizedEnglish = query,
            SearchTerms = BuildSearchTerms(query),
            LanguageDetected = "mixed"
        };

        ApplyDeterministicPriceHints(intent, query);

        return intent;
    }

    private static List<string> BuildSearchTerms(string query)
    {
        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string CleanJson(string content)
    {
        return content
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
    }

    private static string BasicFrancoToArabic(string text)
    {
        return text
            .Replace("2", "ا")
            .Replace("3", "ع")
            .Replace("5", "خ")
            .Replace("7", "ح")
            .Replace("8", "ق")
            .Replace("9", "ص");
    }

    private static void ApplyDeterministicPriceHints(AiSearchIntentDto intent, string query)
    {
        var normalized = NormalizeDigits(query).ToLowerInvariant();

        var maxPrice = FindPrice(normalized, MaxPricePatterns);
        if (maxPrice.HasValue)
        {
            intent.MaxPrice = intent.MaxPrice.HasValue
                ? Math.Min(intent.MaxPrice.Value, maxPrice.Value)
                : maxPrice.Value;
        }

        var minPrice = FindPrice(normalized, MinPricePatterns);
        if (minPrice.HasValue)
        {
            intent.MinPrice = intent.MinPrice.HasValue
                ? Math.Max(intent.MinPrice.Value, minPrice.Value)
                : minPrice.Value;
        }
    }

    private static decimal? FindPrice(string query, IEnumerable<Regex> patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = pattern.Match(query);
            if (!match.Success)
                continue;

            var raw = match.Groups["price"].Value.Replace(",", "");
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price))
                return price;
        }

        return null;
    }

    private static string NormalizeDigits(string text)
    {
        return text
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9')
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9');
    }
}
