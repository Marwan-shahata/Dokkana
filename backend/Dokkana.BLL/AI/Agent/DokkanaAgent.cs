using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agent
{
    // TODO (Sprint 2): wire up Semantic Kernel + OpenAI (gpt-4o-mini) here and
    // register the plugins (ProductSearch, CatalogRag, BusinessCopilot). For now
    // this is a stub so the solution builds end-to-end.
    public class DokkanaAgent : IDokkanaAgent
    {
        public Task<AiSearchResponseDTO> AskAsync(AiSearchRequestDTO request)
        {
            return Task.FromResult(new AiSearchResponseDTO
            {
                Query = request.Query,
                Answer = "AI agent not implemented yet. This is a placeholder response."
            });
        }
    }
}
