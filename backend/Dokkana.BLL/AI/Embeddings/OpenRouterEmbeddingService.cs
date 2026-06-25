using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dokkana.BLL.AI.OpenRouter;
using Microsoft.Extensions.Options;

namespace Dokkana.BLL.AI.Embeddings;

public class OpenRouterEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterEmbeddingService(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<double[]> EmbedOneAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var result = await EmbedManyAsync(
            new List<string> { text },
            cancellationToken);

        return result.FirstOrDefault() ?? Array.Empty<double>();
    }

    public async Task<IReadOnlyList<double[]>> EmbedManyAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
            return new List<double[]>();

        return await EmbedInputsAsync(texts, cancellationToken);
    }

    public async Task<double[]> EmbedImageAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return Array.Empty<double>();

        // The multimodal embedding model accepts an image as a data URI input and
        // returns a vector in the same space as text embeddings.
        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var dataUri = $"data:{mime};base64,{Convert.ToBase64String(imageBytes)}";

        var result = await EmbedInputsAsync(new[] { dataUri }, cancellationToken);
        return result.FirstOrDefault() ?? Array.Empty<double>();
    }

    // Shared call to the OpenRouter embeddings endpoint for any inputs — text
    // strings or image data URIs (the model is multimodal).
    private async Task<List<double[]>> EmbedInputsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenRouter API key is missing.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl.TrimEnd('/')}/embeddings");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.SiteUrl))
            request.Headers.TryAddWithoutValidation("HTTP-Referer", _options.SiteUrl);

        if (!string.IsNullOrWhiteSpace(_options.SiteName))
            request.Headers.TryAddWithoutValidation("X-OpenRouter-Title", _options.SiteName);

        request.Content = JsonContent.Create(new
        {
            model = _options.EmbeddingModel,
            input = inputs
        });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouter embedding error: {response.StatusCode} - {body}");
        }

        using var doc = JsonDocument.Parse(body);

        var data = doc.RootElement.GetProperty("data");

        var embeddings = new List<double[]>();

        foreach (var item in data.EnumerateArray())
        {
            var embeddingArray = item.GetProperty("embedding");

            var vector = embeddingArray
                .EnumerateArray()
                .Select(x => x.GetDouble())
                .ToArray();

            embeddings.Add(vector);
        }

        return embeddings;
    }
}