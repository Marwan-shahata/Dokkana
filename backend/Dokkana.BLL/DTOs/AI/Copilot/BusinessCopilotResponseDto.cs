namespace Dokkana.BLL.DTOs.AI.Copilot;

public class BusinessCopilotResponseDto
{
    public string Question { get; set; } = string.Empty;

    public BusinessIntentDto Intent { get; set; } = new();

    public string Answer { get; set; } = string.Empty;

    public object? Data { get; set; }
}