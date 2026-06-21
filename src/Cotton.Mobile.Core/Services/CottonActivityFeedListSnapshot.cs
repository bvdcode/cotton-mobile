namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedListSnapshot
    {
        private CottonActivityFeedListSnapshot(
            string summaryText,
            string emptyMessage,
            string emptyDetails,
            IReadOnlyList<CottonActivityFeedListItem> items)
        {
            SummaryText = summaryText;
            EmptyMessage = emptyMessage;
            EmptyDetails = emptyDetails;
            Items = items;
        }

        public string SummaryText { get; }

        public string EmptyMessage { get; }

        public string EmptyDetails { get; }

        public IReadOnlyList<CottonActivityFeedListItem> Items { get; }

        public static CottonActivityFeedListSnapshot Create(
            CottonActivityFeedPageSnapshot page,
            TimeZoneInfo displayTimeZone)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(displayTimeZone);

            IReadOnlyList<CottonActivityFeedListItem> items = page.Items
                .Select(item => CottonActivityFeedListItem.Create(item, displayTimeZone))
                .ToArray();
            return new CottonActivityFeedListSnapshot(
                CreateSummaryText(items, page.TotalItemCount),
                "No activity yet",
                "Nothing needs attention right now.",
                items);
        }

        public static string CreateSummaryText(
            IReadOnlyList<CottonActivityFeedListItem> items,
            int? totalItemCount)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (totalItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalItemCount));
            }

            int unreadCount = items.Count(item => item.IsUnread);
            string itemText = totalItemCount > items.Count
                ? $"{items.Count:N0} of {totalItemCount.Value:N0} items"
                : items.Count == 1
                    ? "1 item"
                    : $"{items.Count:N0} items";
            return unreadCount == 0
                ? itemText
                : $"{itemText} · {unreadCount:N0} unread";
        }
    }
}
