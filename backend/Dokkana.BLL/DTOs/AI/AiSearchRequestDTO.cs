namespace Dokkana.BLL.DTOs.AI
{
    // A natural-language shopper query, e.g. "red linen shirt under 300 EGP".
    public class AiSearchRequestDTO
    {
        public string Query { get; set; } = string.Empty;
        public int? StoreId { get; set; }
    }
}
