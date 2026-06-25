namespace Dokkana.BLL.DTOs.AI;

public class AiWebResultDto
{
    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Snippet { get; set; }

    public double Score { get; set; }

    public string Reason { get; set; } = string.Empty;
}