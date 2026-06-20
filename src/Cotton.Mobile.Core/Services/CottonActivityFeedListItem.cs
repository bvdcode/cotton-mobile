namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedListItem
    {
        private CottonActivityFeedListItem(
            Guid id,
            string title,
            string? contentText,
            string detailText,
            string badgeText,
            bool isUnread)
        {
            Id = id;
            Title = title;
            ContentText = contentText;
            DetailText = detailText;
            BadgeText = badgeText;
            IsUnread = isUnread;
        }

        public Guid Id { get; }

        public string Title { get; }

        public string? ContentText { get; }

        public string DetailText { get; }

        public string BadgeText { get; }

        public bool IsUnread { get; }

        public bool IsContentVisible => !string.IsNullOrWhiteSpace(ContentText);

        public static CottonActivityFeedListItem Create(
            CottonActivityFeedItemSnapshot item,
            TimeZoneInfo displayTimeZone)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(displayTimeZone);

            return new CottonActivityFeedListItem(
                item.Id,
                item.Title,
                item.Content,
                CreateDetailText(item, displayTimeZone),
                CreateBadgeText(item),
                item.IsUnread);
        }

        private static string CreateDetailText(
            CottonActivityFeedItemSnapshot item,
            TimeZoneInfo displayTimeZone)
        {
            DateTime utc = CottonLocalFileFreshness.NormalizeUtc(item.CreatedAt);
            DateTime displayTime = TimeZoneInfo.ConvertTimeFromUtc(utc, displayTimeZone);
            return $"{displayTime:yyyy-MM-dd HH:mm}";
        }

        private static string CreateBadgeText(CottonActivityFeedItemSnapshot item)
        {
            if (item.IsUnread)
            {
                return "New";
            }

            return item.Priority switch
            {
                CottonActivityFeedPriority.High => "High",
                CottonActivityFeedPriority.Medium => "Medium",
                CottonActivityFeedPriority.Low => "Low",
                CottonActivityFeedPriority.None => "Info",
                _ => "Info",
            };
        }
    }
}
