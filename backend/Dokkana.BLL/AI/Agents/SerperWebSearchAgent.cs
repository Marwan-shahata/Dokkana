using System.Net.Http.Json;
using System.Text.Json;
using Dokkana.BLL.AI.WebSearch;
using Dokkana.BLL.DTOs.AI;
using Microsoft.Extensions.Options;

namespace Dokkana.BLL.AI.Agents;

public class SerperWebSearchAgent : IWebSearchAgent
{
    private readonly HttpClient _httpClient;
    private readonly SerperOptions _options;

    public SerperWebSearchAgent(
        HttpClient httpClient,
        IOptions<SerperOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<AiWebResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return BuildFallbackShoppingLinks(intent);

        var query = BuildWebQuery(intent);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            _options.Endpoint);

        request.Headers.Add("X-API-KEY", _options.ApiKey);

        request.Content = JsonContent.Create(new
        {
            q = query,
            gl = "eg",
            hl = intent.LanguageDetected.Equals("english", StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "ar",
            num = 5
        });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new List<AiWebResultDto>();

        using var json = JsonDocument.Parse(body);

        if (!json.RootElement.TryGetProperty("organic", out var organicResults))
            return new List<AiWebResultDto>();

        var results = new List<AiWebResultDto>();

        foreach (var item in organicResults.EnumerateArray())
        {
            var title = item.TryGetProperty("title", out var titleProp)
                ? titleProp.GetString()
                : null;

            var link = item.TryGetProperty("link", out var linkProp)
                ? linkProp.GetString()
                : null;

            var snippet = item.TryGetProperty("snippet", out var snippetProp)
                ? snippetProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                continue;

            results.Add(new AiWebResultDto
            {
                Title = title,
                Url = link,
                Snippet = snippet,
                Score = 0,
                Reason = "نتيجة من البحث على الإنترنت"
            });
        }

        return results;
    }

    // When Serper is not configured, still give shoppers useful online buying
    // paths instead of an empty web section. These are search-result links, not
    // fetched product cards. Adding Serper:ApiKey upgrades this to real web
    // results automatically.
    private static List<AiWebResultDto> BuildFallbackShoppingLinks(AiSearchIntentDto intent)
    {
        var query = BuildWebQuery(intent);
        var encoded = Uri.EscapeDataString(query);

        return new List<AiWebResultDto>
        {
            new()
            {
                Title = $"Amazon Egypt results for {intent.RawQuery}",
                Url = $"https://www.amazon.eg/s?k={encoded}",
                Snippet = "Open Amazon Egypt search results for matching online products.",
                Reason = "رابط بحث للتسوق على الإنترنت"
            },
            new()
            {
                Title = $"Noon Egypt results for {intent.RawQuery}",
                Url = $"https://www.noon.com/egypt-en/search/?q={encoded}",
                Snippet = "Open Noon Egypt search results for related products.",
                Reason = "رابط بحث للتسوق على الإنترنت"
            },
            new()
            {
                Title = $"Jumia Egypt results for {intent.RawQuery}",
                Url = $"https://www.jumia.com.eg/catalog/?q={encoded}",
                Snippet = "Open Jumia Egypt search results for related products.",
                Reason = "رابط بحث للتسوق على الإنترنت"
            },
            new()
            {
                Title = $"Google Shopping results for {intent.RawQuery}",
                Url = $"https://www.google.com/search?tbm=shop&q={encoded}",
                Snippet = "Open Google Shopping results across online sellers.",
                Reason = "رابط بحث للتسوق على الإنترنت"
            }
        };
    }

    private static string BuildWebQuery(AiSearchIntentDto intent)
    {
        var terms = new List<string>();

        AddSearchTerm(terms, intent.NormalizedEnglish);
        AddSearchTerm(terms, intent.NormalizedArabic);
        AddSearchTerm(terms, intent.RawQuery);
        AddSearchTerm(terms, intent.ProductType);
        AddSearchTerm(terms, intent.Color);
        AddSearchTerm(terms, intent.Size);
        AddSearchTerm(terms, intent.Gender);

        terms.Add("fashion Egypt local stores");

        return string.Join(" ", terms);
    }

    private static void AddSearchTerm(List<string> terms, string? term)
    {
        var cleaned = term?.Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            return;

        if (terms.Any(existing =>
                existing.Contains(cleaned, StringComparison.OrdinalIgnoreCase) ||
                cleaned.Contains(existing, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        terms.Add(cleaned);
    }
}
