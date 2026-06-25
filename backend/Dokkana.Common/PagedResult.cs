namespace Dokkana.Common
{
    // Wrapper for paginated list endpoints (e.g. product listings).
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
            => new() { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
    }
}
