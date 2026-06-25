namespace Dokkana.BLL.AI.OpenRouter;

public interface IOpenRouterClient
{
    // True when an API key is configured (lets callers fall back gracefully).
    bool IsConfigured { get; }

    Task<string> AskAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    // Multimodal call: sends a text prompt plus an image (public URL or a
    // data: URL) to the configured vision model and returns the text reply.
    Task<string> AskWithImageAsync(
        string systemPrompt,
        string userPrompt,
        string imageUrl,
        CancellationToken cancellationToken = default);
}