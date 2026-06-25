namespace Dokkana.BLL.DTOs.AI.Agent;

public class DokkanaAgentRequestDto
{
    public string Message { get; set; } = string.Empty;

    // Required for catalog/copilot.
    // Optional for product search.
    public int? StoreId { get; set; }

    // Shopper search options.
    public bool IncludeWeb { get; set; } = true;

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public double? UserLat { get; set; }

    public double? UserLng { get; set; }

    // Catalog RAG option.
    public int MaxChunks { get; set; } = 5;

    // Optional hint from frontend:
    // shopper / store_owner / admin
    public string? UserRole { get; set; }
}