namespace Dokkana.BLL.DTOs.AI;

public class AiProductResultDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public bool IsOnSale { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? StoreName { get; set; }

    public string? ImageUrl { get; set; }

    // Where to buy: store location (so the assistant can show address / map pin).
    public int StoreId { get; set; }

    public string? StoreAddress { get; set; }

    public double? StoreLatitude { get; set; }

    public double? StoreLongitude { get; set; }

    // Distance from the shopper in km, when userLat/userLng were sent in the request.
    public double? DistanceKm { get; set; }

    // Average shopper rating (0-5) and how many reviews it is based on.
    public double Rating { get; set; }

    public int ReviewCount { get; set; }

    // Cosine similarity (0-1) from the semantic/vector search, when the product
    // was matched by meaning rather than (or in addition to) keywords.
    public double SemanticScore { get; set; }

    public double Score { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsSponsored { get; set; }

    public int? PromotionId { get; set; }
}
