using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public interface ILocalSearchAgent
{
    Task<List<AiProductResultDto>> SearchAsync(
        AiSearchIntentDto intent,
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default);
}