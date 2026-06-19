namespace Cotton.Mobile.Services
{
    public static class CottonCachedFolderListingNoticeText
    {
        private const string CurrentListingMessage = "Files marked On device can still open.";
        private const string MissingListingMessage = "Reconnect to load this folder.";

        public static string CreateMessage(
            int visibleEntryCount,
            DateTime? cachedAtUtc,
            DateTime nowUtc)
        {
            if (!cachedAtUtc.HasValue)
            {
                return visibleEntryCount > 0 ? CurrentListingMessage : MissingListingMessage;
            }

            string age = FormatAge(cachedAtUtc.Value, nowUtc);
            if (visibleEntryCount == 0)
            {
                return $"Saved folder list is empty. Cached {age}. Reconnect to refresh.";
            }

            return $"Saved folder list cached {age}. Files marked On device can still open.";
        }

        public static string FormatAge(DateTime cachedAtUtc, DateTime nowUtc)
        {
            DateTime cachedAt = CottonLocalFileFreshness.NormalizeUtc(cachedAtUtc);
            DateTime now = CottonLocalFileFreshness.NormalizeUtc(nowUtc);
            TimeSpan age = now - cachedAt;
            if (age < TimeSpan.Zero)
            {
                age = TimeSpan.Zero;
            }

            if (age < TimeSpan.FromMinutes(1))
            {
                return "just now";
            }

            if (age < TimeSpan.FromHours(1))
            {
                int minutes = Math.Max(1, (int)Math.Floor(age.TotalMinutes));
                return FormatCount(minutes, "minute");
            }

            if (age < TimeSpan.FromDays(1))
            {
                int hours = Math.Max(1, (int)Math.Floor(age.TotalHours));
                return FormatCount(hours, "hour");
            }

            int days = Math.Max(1, (int)Math.Floor(age.TotalDays));
            return FormatCount(days, "day");
        }

        private static string FormatCount(int count, string unit)
        {
            return count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
        }
    }
}
