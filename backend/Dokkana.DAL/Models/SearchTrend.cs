namespace Dokkana.DAL.Models
{
    public class SearchTrend
    {
        public int Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public string NormalizedQuery { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;
    }
}
