using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public interface IAiSearchManager
{
    Task<AiSearchResponseDto> SearchAsync(
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default);
}