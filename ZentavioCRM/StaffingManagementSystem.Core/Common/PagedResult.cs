namespace ZentavioCRM.Core.Common
{
    /// <summary>Standard paged list envelope returned by every list endpoint.</summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
