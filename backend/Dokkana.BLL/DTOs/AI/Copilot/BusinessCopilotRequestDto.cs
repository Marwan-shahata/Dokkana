namespace Dokkana.BLL.DTOs.AI.Copilot;

public class BusinessCopilotRequestDto
{
    public int StoreId { get; set; }

    public string Question { get; set; } = string.Empty;
}