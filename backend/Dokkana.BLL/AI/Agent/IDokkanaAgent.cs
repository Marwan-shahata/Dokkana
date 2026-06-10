using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agent
{
    // Single entry point the controllers use for all AI features. Implemented by
    // DokkanaAgent (one Semantic Kernel agent with several plugins/tools).
    public interface IDokkanaAgent
    {
        Task<AiSearchResponseDTO> AskAsync(AiSearchRequestDTO request);
    }
}
