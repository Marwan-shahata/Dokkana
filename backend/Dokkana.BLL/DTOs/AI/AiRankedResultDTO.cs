namespace Dokkana.BLL.DTOs.AI;

public class AiRankedResultDto
{
    public string Source { get; set; } = string.Empty;
    // local / web

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public bool IsOnSale { get; set; }

    public string? ImageUrl { get; set; }

    public string? StoreName { get; set; }

    public string? Url { get; set; }

    public double Score { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsSponsored { get; set; }

    public int? PromotionId { get; set; }

    public AiProductResultDto? Product { get; set; }

    public AiWebResultDto? WebResult { get; set; }
}
