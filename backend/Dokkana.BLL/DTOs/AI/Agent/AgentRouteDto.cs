namespace Dokkana.BLL.DTOs.AI.Agent;

public class AgentRouteDto
{
    public string Route { get; set; } = "general";
    // product_search / catalog_rag / business_copilot / general

    public double Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;
}