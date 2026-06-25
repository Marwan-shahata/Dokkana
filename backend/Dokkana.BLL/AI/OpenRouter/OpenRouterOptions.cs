namespace Dokkana.BLL.AI.OpenRouter;

public class OpenRouterOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "openrouter/free";

    // Multimodal model used for product image auto-tagging (MM1). Override in
    // config if a different free/paid vision model is preferred.
    public string VisionModel { get; set; } = "meta-llama/llama-3.2-11b-vision-instruct:free";

    public string EmbeddingModel { get; set; } =
        "nvidia/llama-nemotron-embed-vl-1b-v2:free";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    public string? SiteUrl { get; set; }

    public string? SiteName { get; set; } = "Dokkana";
}