using Dokkana.BLL.DTOs.Product;

namespace Dokkana.BLL.DTOs.AI
{
    public class AiSearchResponseDTO
    {
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        // Matched product cards so the frontend can render real results, not just
        // the answer text. Populated by a keyword/price match over the catalog.
        public IReadOnlyList<ProductListDTO> Products { get; set; } = new List<ProductListDTO>();
        public int TotalMatches { get; set; }
    }
}
