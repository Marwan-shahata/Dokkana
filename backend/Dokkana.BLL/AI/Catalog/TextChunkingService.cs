namespace Dokkana.BLL.AI.Catalog;

public class TextChunkingService : ITextChunkingService
{
    public List<string> SplitIntoChunks(string text, int maxChunkLength = 1200)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        var paragraphs = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var current = "";

        foreach (var paragraph in paragraphs)
        {
            if ((current.Length + paragraph.Length) > maxChunkLength)
            {
                if (!string.IsNullOrWhiteSpace(current))
                    chunks.Add(current.Trim());

                current = paragraph;
            }
            else
            {
                current += Environment.NewLine + paragraph;
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            chunks.Add(current.Trim());

        return chunks;
    }
}