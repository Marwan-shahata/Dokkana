using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.DTOs.AI.Agent;

public class DokkanaAgentResponseDto
{
    public string Message { get; set; } = string.Empty;

    public AgentRouteDto Route { get; set; } = new();

    public string Answer { get; set; } = string.Empty;

    public AiSearchResponseDto? ProductSearch { get; set; }

    public CatalogQuestionResponseDto? Catalog { get; set; }

    public BusinessCopilotResponseDto? Copilot { get; set; }
}