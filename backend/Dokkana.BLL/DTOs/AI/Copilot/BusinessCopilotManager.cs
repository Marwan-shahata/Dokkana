using System.Text.Json;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public class BusinessCopilotManager : IBusinessCopilotManager
{
    private readonly IBusinessIntentAgent _businessIntentAgent;
    private readonly ISalesAnalyticsManager _salesAnalyticsManager;
    private readonly IInventoryAnalyticsManager _inventoryAnalyticsManager;
    private readonly IOpenRouterClient _openRouterClient;

    public BusinessCopilotManager(
        IBusinessIntentAgent businessIntentAgent,
        ISalesAnalyticsManager salesAnalyticsManager,
        IInventoryAnalyticsManager inventoryAnalyticsManager,
        IOpenRouterClient openRouterClient)
    {
        _businessIntentAgent = businessIntentAgent;
        _salesAnalyticsManager = salesAnalyticsManager;
        _inventoryAnalyticsManager = inventoryAnalyticsManager;
        _openRouterClient = openRouterClient;
    }

    public async Task<BusinessCopilotResponseDto> AskAsync(
        BusinessCopilotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.StoreId <= 0)
        {
            return new BusinessCopilotResponseDto
            {
                Question = request.Question,
                Answer = "StoreId غير صحيح."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new BusinessCopilotResponseDto
            {
                Question = request.Question,
                Answer = "اكتب سؤالك عن المبيعات أو المخزون."
            };
        }

        var intent = await _businessIntentAgent.DetectIntentAsync(
            request.Question,
            cancellationToken);

        var (from, to) = DateRangeHelper.Resolve(intent.Period);

        object data;

        switch (intent.Intent)
        {
            case "top_selling_products":
                data = await _salesAnalyticsManager.GetTopSellingProductsAsync(
                    request.StoreId,
                    from,
                    to,
                    intent.Limit,
                    cancellationToken);
                break;

            case "low_stock_products":
                data = await _inventoryAnalyticsManager.GetLowStockProductsAsync(
                    request.StoreId,
                    intent.LowStockThreshold,
                    intent.Limit,
                    cancellationToken);
                break;

            case "unsold_products":
                data = await _salesAnalyticsManager.GetUnsoldProductsAsync(
                    request.StoreId,
                    from,
                    to,
                    intent.Limit,
                    cancellationToken);
                break;

            case "sales_summary":
            default:
                data = await _salesAnalyticsManager.GetSalesSummaryAsync(
                    request.StoreId,
                    from,
                    to,
                    cancellationToken);
                break;
        }

        var answer = await BuildAnswerAsync(
            request.Question,
            intent,
            data,
            cancellationToken);

        return new BusinessCopilotResponseDto
        {
            Question = request.Question,
            Intent = intent,
            Data = data,
            Answer = answer
        };
    }

    private async Task<string> BuildAnswerAsync(
        string question,
        BusinessIntentDto intent,
        object data,
        CancellationToken cancellationToken)
    {
        var systemPrompt = """
You are a business copilot for a local fashion store owner.

Answer in the same language as the user's question.
If the user writes Arabic or Franco-Arabic, answer in simple Egyptian Arabic.
If the user writes English, answer in English.

Rules:
- Use only the provided JSON data.
- Do not invent numbers.
- Do not invent product names.
- If data is empty, clearly say there is not enough data yet.
- Keep the answer short and useful.
- Give one practical recommendation if possible.
""";

        var userPrompt = $"""
Question:
{question}

Detected intent:
{JsonSerializer.Serialize(intent)}

Data:
{JsonSerializer.Serialize(data)}
""";

        try
        {
            var answer = await _openRouterClient.AskAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            return string.IsNullOrWhiteSpace(answer)
                ? BuildFallbackAnswer(intent, data)
                : answer.Trim();
        }
        catch
        {
            return BuildFallbackAnswer(intent, data);
        }
    }

    private static string BuildFallbackAnswer(
        BusinessIntentDto intent,
        object data)
    {
        var json = JsonSerializer.Serialize(data);

        return intent.Intent switch
        {
            "top_selling_products" =>
                $"دي أكتر المنتجات مبيعًا حسب البيانات المتاحة: {json}",

            "low_stock_products" =>
                $"دي المنتجات اللي مخزونها قليل حسب الحد المطلوب: {json}",

            "unsold_products" =>
                $"دي منتجات لم تُسجل لها مبيعات في الفترة المطلوبة: {json}",

            _ =>
                $"ده ملخص المبيعات حسب البيانات المتاحة: {json}"
        };
    }
}