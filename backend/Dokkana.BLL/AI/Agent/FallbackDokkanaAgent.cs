using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agent
{
    // Used only when no OpenAI key is configured, so the API still runs and the
    // AI endpoint returns a clear message instead of crashing. The real agent
    // (DokkanaAgent) is registered when OpenAI:ApiKey is present.
    public class FallbackDokkanaAgent : IDokkanaAgent
    {
        public Task<AiSearchResponseDTO> AskAsync(
            AiSearchRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiSearchResponseDTO
            {
                Query = request?.Query ?? string.Empty,
                Answer = "AI search is not configured. Set OpenAI:ApiKey via user-secrets to enable it."
            });
        }
    }
}
