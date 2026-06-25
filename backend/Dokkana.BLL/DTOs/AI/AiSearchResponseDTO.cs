namespace Dokkana.BLL.DTOs.AI;

public class AiSearchResponseDto
{
    public string Query { get; set; } = string.Empty;

    public AiSearchIntentDto Intent { get; set; } = new();

    public string Answer { get; set; } = string.Empty;

    public List<AiProductResultDto> LocalResults { get; set; } = new();

    public List<AiWebResultDto> WebResults { get; set; } = new();

    public List<AiRankedResultDto> RankedResults { get; set; } = new();
}