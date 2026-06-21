using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ActivityFeedPresentationTests
    {
        private static readonly DateTime CreatedAt =
            new(2026, 6, 20, 4, 45, 0, DateTimeKind.Utc);
        private static readonly TimeZoneInfo DisplayTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "CottonActivityDisplay",
            TimeSpan.FromHours(3),
            "Cotton activity display",
            "Cotton activity display");

        [Fact]
        public void List_snapshot_formats_summary_and_items()
        {
            CottonActivityFeedPageSnapshot page = new(
                new CottonActivityFeedQuery(pageSize: 20),
                [
                    CreateItem(
                        "Shared file downloaded",
                        "Report.pdf was downloaded.",
                        CreatedAt,
                        readAt: null,
                        CottonActivityFeedPriority.Medium),
                    CreateItem(
                        "Security",
                        null,
                        CreatedAt.AddMinutes(-5),
                        readAt: CreatedAt,
                        CottonActivityFeedPriority.High),
                ]);

            CottonActivityFeedListSnapshot snapshot =
                CottonActivityFeedListSnapshot.Create(page, DisplayTimeZone);

            Assert.Equal("2 items · 1 unread", snapshot.SummaryText);
            CottonActivityFeedListItem first = snapshot.Items[0];
            Assert.Equal("Shared file downloaded", first.Title);
            Assert.Equal("Report.pdf was downloaded.", first.ContentText);
            Assert.True(first.IsContentVisible);
            Assert.Equal("2026-06-20 07:45", first.DetailText);
            Assert.Equal("New", first.BadgeText);

            CottonActivityFeedListItem second = snapshot.Items[1];
            Assert.Equal("Security", second.Title);
            Assert.False(second.IsContentVisible);
            Assert.Equal("2026-06-20 07:40", second.DetailText);
            Assert.Equal("High", second.BadgeText);
        }

        [Fact]
        public void List_snapshot_formats_empty_state()
        {
            CottonActivityFeedPageSnapshot page = new(
                new CottonActivityFeedQuery(),
                []);

            CottonActivityFeedListSnapshot snapshot =
                CottonActivityFeedListSnapshot.Create(page, TimeZoneInfo.Utc);

            Assert.Equal("0 items", snapshot.SummaryText);
            Assert.Equal("No activity yet", snapshot.EmptyMessage);
            Assert.Equal("Nothing needs attention right now.", snapshot.EmptyDetails);
            Assert.Empty(snapshot.Items);
        }

        [Fact]
        public void List_snapshot_formats_loaded_total_count()
        {
            CottonActivityFeedPageSnapshot page = new(
                new CottonActivityFeedQuery(pageSize: 2),
                [
                    CreateItem(
                        "Shared file downloaded",
                        null,
                        CreatedAt,
                        readAt: null,
                        CottonActivityFeedPriority.Medium),
                    CreateItem(
                        "Security",
                        null,
                        CreatedAt.AddMinutes(-5),
                        readAt: CreatedAt,
                        CottonActivityFeedPriority.High),
                ],
                totalItemCount: 5);

            CottonActivityFeedListSnapshot snapshot =
                CottonActivityFeedListSnapshot.Create(page, DisplayTimeZone);

            Assert.Equal("2 of 5 items · 1 unread", snapshot.SummaryText);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonActivityFeedListSnapshot.CreateSummaryText(snapshot.Items, totalItemCount: -1));
        }

        [Fact]
        public void List_item_formats_read_priority_badges()
        {
            Assert.Equal(
                "Medium",
                CottonActivityFeedListItem.Create(
                    CreateItem("Medium", null, CreatedAt, CreatedAt, CottonActivityFeedPriority.Medium),
                    TimeZoneInfo.Utc).BadgeText);
            Assert.Equal(
                "Low",
                CottonActivityFeedListItem.Create(
                    CreateItem("Low", null, CreatedAt, CreatedAt, CottonActivityFeedPriority.Low),
                    TimeZoneInfo.Utc).BadgeText);
            Assert.Equal(
                "Info",
                CottonActivityFeedListItem.Create(
                    CreateItem("Info", null, CreatedAt, CreatedAt, CottonActivityFeedPriority.None),
                    TimeZoneInfo.Utc).BadgeText);
        }

        private static CottonActivityFeedItemSnapshot CreateItem(
            string title,
            string? content,
            DateTime createdAt,
            DateTime? readAt,
            CottonActivityFeedPriority priority)
        {
            return new CottonActivityFeedItemSnapshot(
                Guid.NewGuid(),
                title,
                content,
                createdAt,
                readAt,
                priority,
                null);
        }
    }
}
