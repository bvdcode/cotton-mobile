using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CachedFolderListingNoticeTests
    {
        private static readonly DateTime Now = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(0, "just now")]
        [InlineData(59, "just now")]
        [InlineData(60, "1 minute ago")]
        [InlineData(120, "2 minutes ago")]
        [InlineData(3600, "1 hour ago")]
        [InlineData(7200, "2 hours ago")]
        [InlineData(86400, "1 day ago")]
        [InlineData(172800, "2 days ago")]
        public void Age_text_uses_coarse_stable_units(int secondsOld, string expected)
        {
            DateTime cachedAt = Now.AddSeconds(-secondsOld);

            Assert.Equal(expected, CottonCachedFolderListingNoticeText.FormatAge(cachedAt, Now));
        }

        [Fact]
        public void Age_text_clamps_future_cache_times_to_just_now()
        {
            Assert.Equal(
                "just now",
                CottonCachedFolderListingNoticeText.FormatAge(Now.AddMinutes(5), Now));
        }

        [Fact]
        public void Notice_copy_includes_cache_age_for_non_empty_saved_listing()
        {
            string message = CottonCachedFolderListingNoticeText.CreateMessage(
                visibleEntryCount: 3,
                cachedAtUtc: Now.AddMinutes(-5),
                nowUtc: Now);

            Assert.Equal(
                "Saved folder list cached 5 minutes ago. Files marked On device can still open.",
                message);
        }

        [Fact]
        public void Notice_copy_keeps_empty_cached_listing_honest()
        {
            string message = CottonCachedFolderListingNoticeText.CreateMessage(
                visibleEntryCount: 0,
                cachedAtUtc: Now.AddHours(-2),
                nowUtc: Now);

            Assert.Equal(
                "Saved folder list is empty. Cached 2 hours ago. Reconnect to refresh.",
                message);
        }

        [Fact]
        public void Notice_copy_without_cache_time_preserves_current_offline_copy()
        {
            Assert.Equal(
                "Files marked On device can still open.",
                CottonCachedFolderListingNoticeText.CreateMessage(visibleEntryCount: 2, cachedAtUtc: null, nowUtc: Now));
            Assert.Equal(
                "Reconnect to load this folder.",
                CottonCachedFolderListingNoticeText.CreateMessage(visibleEntryCount: 0, cachedAtUtc: null, nowUtc: Now));
        }
    }
}
