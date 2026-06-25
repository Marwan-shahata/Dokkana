using System.Text;
using UglyToad.PdfPig;

namespace Dokkana.BLL.AI.Catalog;

public class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();

        await pdfStream.CopyToAsync(memoryStream, cancellationToken);

        memoryStream.Position = 0;

        var builder = new StringBuilder();

        using var document = PdfDocument.Open(memoryStream);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            builder.AppendLine($"--- Page {page.Number} ---");
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}