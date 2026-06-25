using System.Text.Json;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI.Agent;

namespace Dokkana.BLL.AI.Orchestrator;

public class RouteClassifierAgent : IRouteClassifierAgent
{
    private readonly IOpenRouterClient _openRouterClient;

    public RouteClassifierAgent(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    public async Task<AgentRouteDto> ClassifyAsync(
        DokkanaAgentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new AgentRouteDto
            {
                Route = "general",
                Confidence = 1,
                Reason = "Empty message"
            };
        }

        var systemPrompt = """
You are a route classifier for Dokkana, an AI-powered local fashion discovery platform.

The user may write in:
- Arabic
- English
- Franco-Arabic / Arabizi
- Mixed Arabic and English

You must choose exactly one route:

1) product_search
Use this when the user wants to find or compare fashion products.
Examples:
- عايز تيشيرت اسود
- black hoodie under 700
- 3ayz kotchy abyad
- find me a red dress
- أرخص كوتشي أبيض

2) catalog_rag
Use this when the user asks about uploaded catalog/PDF/catalog policies/catalog contents.
Examples:
- اسأل الكتالوج
- هل موجود في الكتالوج؟
- هل PDF فيه قميص كتان؟
- هل الاستبدال متاح؟
- what does the catalog say about exchange policy?
- catalog prices

3) business_copilot
Use this when the store owner asks about sales, orders, revenue, stock, inventory, best sellers, low stock, unsold products, completed orders.
Examples:
- إيه أكتر منتج اتباع الشهر ده؟
- ملخص المبيعات الأسبوع ده
- المنتجات اللي قربت تخلص
- which products are not selling?
- revenue this month
- كام order completed النهاردة؟

4) general
Use this only if the message is not related to product search, catalog Q&A, or store business analytics.

Rules:
- Return JSON only.
- Do not write markdown.
- Do not explain outside JSON.
- If the question contains sales, revenue, stock, low stock, orders, best sellers => business_copilot.
- If the question contains catalog, pdf, كتالوج, policy, سياسة, استبدال, استرجاع => catalog_rag.
- If the question is asking to find clothes/products => product_search.
- If unsure between product_search and catalog_rag, choose product_search unless the user explicitly mentions catalog/pdf.
- If unsure between business_copilot and product_search, choose business_copilot if the user speaks as a store owner about "my stock", "my sales", "عندي قرب يخلص".

Return exactly:
{
  "route": "product_search",
  "confidence": 0.0,
  "reason": ""
}
""";

        var userPrompt = $"""
Message:
{request.Message}

UserRole:
{request.UserRole ?? "unknown"}

StoreId:
{request.StoreId?.ToString() ?? "null"}
""";

        try
        {
            var content = await _openRouterClient.AskAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            content = CleanJson(content);

            var route = JsonSerializer.Deserialize<AgentRouteDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return Normalize(route, request.Message);
        }
        catch
        {
            return FallbackClassify(request.Message);
        }
    }

    private static AgentRouteDto Normalize(
        AgentRouteDto? route,
        string message)
    {
        if (route == null)
            return FallbackClassify(message);

        var allowed = new HashSet<string>
        {
            "product_search",
            "catalog_rag",
            "business_copilot",
            "general"
        };

        if (!allowed.Contains(route.Route))
            route.Route = "general";

        if (route.Confidence < 0 || route.Confidence > 1)
            route.Confidence = 0.5;

        if (string.IsNullOrWhiteSpace(route.Reason))
            route.Reason = "Classified by route classifier.";

        return route;
    }

    private static AgentRouteDto FallbackClassify(string message)
    {
        var q = message.ToLowerInvariant();

        if (ContainsAny(q,
                "مبيعات", "اتباع", "اتبع", "مخزون", "خلص", "قرب يخلص",
                "revenue", "sales", "orders", "order", "stock", "low stock",
                "best seller", "most sold", "not selling"))
        {
            return new AgentRouteDto
            {
                Route = "business_copilot",
                Confidence = 0.7,
                Reason = "Fallback matched business analytics words."
            };
        }

        if (ContainsAny(q,
                "كتالوج", "catalog", "pdf", "policy", "سياسة", "استبدال", "استرجاع"))
        {
            return new AgentRouteDto
            {
                Route = "catalog_rag",
                Confidence = 0.7,
                Reason = "Fallback matched catalog words."
            };
        }

        if (ContainsAny(q,
                "عايز", "عايزة", "هات", "دور", "ابحث", "find", "search",
                "tshirt", "shirt", "hoodie", "dress", "sneakers", "shoes",
                "تيشيرت", "قميص", "هودي", "فستان", "كوتشي", "جزمة"))
        {
            return new AgentRouteDto
            {
                Route = "product_search",
                Confidence = 0.7,
                Reason = "Fallback matched product search words."
            };
        }

        return new AgentRouteDto
        {
            Route = "general",
            Confidence = 0.5,
            Reason = "Fallback could not match a supported AI feature."
        };
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(text.Contains);
    }

    private static string CleanJson(string content)
    {
        return content
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
    }
}