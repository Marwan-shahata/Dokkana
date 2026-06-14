using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agent
{
    // Single entry point the controllers use for all AI features.
    public interface IDokkanaAgent
    {
        Task<AiSearchResponseDTO> AskAsync(
            AiSearchRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}