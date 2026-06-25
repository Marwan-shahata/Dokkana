namespace Dokkana.BLL.AI.Embeddings;

public interface IEmbeddingService
{
    Task<double[]> EmbedOneAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<double[]>> EmbedManyAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    // Embeds an image into the SAME vector space as text (the model is multimodal),
    // so an image query can be matched against product text embeddings.
    Task<double[]> EmbedImageAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}