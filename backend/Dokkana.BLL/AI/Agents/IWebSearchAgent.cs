using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public interface IWebSearchAgent
{
    Task<List<AiWebResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        CancellationToken cancellationToken = default);
}