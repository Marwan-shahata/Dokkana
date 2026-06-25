namespace Dokkana.BLL.AI.Catalog;

public interface ITextChunkingService
{
    List<string> SplitIntoChunks(string text, int maxChunkLength = 1200);
}