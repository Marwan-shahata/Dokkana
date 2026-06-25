using System.Text.Json;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public class BusinessIntentAgent : IBusinessIntentAgent
{
    private readonly IOpenRouterClient _openRouterClient;

    public BusinessIntentAgent(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    public async Task<BusinessIntentDto> DetectIntentAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
You are a business intent classifier for a local fashion store dashboard.

The store owner may ask in:
- Arabic
- English
- Franco-Arabic
- Mixed language

Supported intents only:
1. top_selling_products
2. sales_summary
3. low_stock_products
4. unsold_products

Supported periods only:
- today
- yesterday
- this_week
- last_week
- this_month
- last_month
- this_year
- all_time

Rules:
- Return JSON only.
- Do not write markdown.
- Do not explain.
- If unsure, use sales_summary.
- If the question asks about products that need restocking, use low_stock_products.
- If the question asks about best sellers, most sold, اكتر منتج اتباع, use top_selling_products.
- If the question asks about products not moving or not selling, use unsold_products.
- If no period is mentioned, use this_month.
- limit default is 5.
- lowStockThreshold default is 5.

Return exactly:
{
  "intent": "sales_summary",
  "period": "this_month",
  "limit": 5,
  "lowStockThreshold": 5
}
""";

        try
        {
            var content = await _openRouterClient.AskAsync(
                systemPrompt,
                question,
                cancellationToken);

            content = CleanJson(content);

            var intent = JsonSerializer.Deserialize<BusinessIntentDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return Normalize(intent, question);
        }
        catch
        {
            return FallbackIntent(question);
        }
    }

    private static BusinessIntentDto Normalize(
        BusinessIntentDto? intent,
        string question)
    {
        if (intent == null)
            return FallbackIntent(question);

        var allowedIntents = new HashSet<string>
        {
            "top_selling_products",
            "sales_summary",
            "low_stock_products",
            "unsold_products"
        };

        var allowedPeriods = new HashSet<string>
        {
            "today",
            "yesterday",
            "this_week",
            "last_week",
            "this_month",
            "last_month",
            "this_year",
            "all_time"
        };

        if (!allowedIntents.Contains(intent.Intent))
            intent.Intent = "sales_summary";

        if (!allowedPeriods.Contains(intent.Period))
            intent.Period = "this_month";

        if (intent.Limit <= 0)
            intent.Limit = 5;

        if (intent.Limit > 20)
            intent.Limit = 20;

        if (intent.LowStockThreshold <= 0)
            intent.LowStockThreshold = 5;

        return intent;
    }

    private static BusinessIntentDto FallbackIntent(string question)
    {
        var q = question.ToLowerInvariant();

        var intent = "sales_summary";

        if (q.Contains("اكتر") ||
            q.Contains("أكتر") ||
            q.Contains("most") ||
            q.Contains("top") ||
            q.Contains("best") ||
            q.Contains("sold"))
        {
            intent = "top_selling_products";
        }
        else if (q.Contains("خلص") ||
                 q.Contains("قرب") ||
                 q.Contains("restock") ||
                 q.Contains("stock") ||
                 q.Contains("low"))
        {
            intent = "low_stock_products";
        }
        else if (q.Contains("مش بيتباع") ||
                 q.Contains("not selling") ||
                 q.Contains("unsold") ||
                 q.Contains("slow"))
        {
            intent = "unsold_products";
        }

        return new BusinessIntentDto
        {
            Intent = intent,
            Period = "this_month",
            Limit = 5,
            LowStockThreshold = 5
        };
    }

    private static string CleanJson(string content)
    {
        return content
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
    }
}