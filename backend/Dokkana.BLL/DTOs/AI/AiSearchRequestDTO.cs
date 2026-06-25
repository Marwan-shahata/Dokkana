namespace Dokkana.BLL.DTOs.AI;

public class AiSearchRequestDto
{
    public string Query { get; set; } = string.Empty;

    public bool IncludeWeb { get; set; } = true;

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public double? UserLat { get; set; }

    public double? UserLng { get; set; }
}