namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedQuery
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;

        public CottonActivityFeedQuery(
            int page = DefaultPage,
            int pageSize = DefaultPageSize)
        {
            if (page <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Activity feed page must be positive.");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Activity feed page size must be positive.");
            }

            Page = page;
            PageSize = pageSize;
        }

        public int Page { get; }

        public int PageSize { get; }

        public string ToQueryString()
        {
            return $"page={Page}&pageSize={PageSize}";
        }
    }
}
