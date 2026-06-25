using Dokkana.BLL.DTOs.AI.Agent;

namespace Dokkana.BLL.AI.Orchestrator;

public interface IDokkanaAgentManager
{
    Task<DokkanaAgentResponseDto> AskAsync(
        DokkanaAgentRequestDto request,
        CancellationToken cancellationToken = default);
}