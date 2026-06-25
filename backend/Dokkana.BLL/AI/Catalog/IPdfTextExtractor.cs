namespace Dokkana.BLL.AI.Catalog;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}