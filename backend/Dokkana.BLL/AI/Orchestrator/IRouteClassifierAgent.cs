using Dokkana.BLL.DTOs.AI.Agent;

namespace Dokkana.BLL.AI.Orchestrator;

public interface IRouteClassifierAgent
{
    Task<AgentRouteDto> ClassifyAsync(
        DokkanaAgentRequestDto request,
        CancellationToken cancellationToken = default);
}