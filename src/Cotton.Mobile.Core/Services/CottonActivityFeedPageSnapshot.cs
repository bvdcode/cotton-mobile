namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedPageSnapshot
    {
        public CottonActivityFeedPageSnapshot(
            CottonActivityFeedQuery query,
            IReadOnlyList<CottonActivityFeedItemSnapshot> items)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(items);

            Query = query;
            Items = items.ToArray();
        }

        public CottonActivityFeedQuery Query { get; }

        public IReadOnlyList<CottonActivityFeedItemSnapshot> Items { get; }

        public bool IsEmpty => Items.Count == 0;

        public bool MayHaveMore => Items.Count == Query.PageSize;
    }
}
