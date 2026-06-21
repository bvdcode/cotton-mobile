using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ActivityFeedPagingStateTests
    {
        private static readonly DateTime CreatedAt =
            new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Empty_state_starts_before_the_first_page()
        {
            CottonActivityFeedPagingState state = CottonActivityFeedPagingState.Empty;

            Assert.Equal(0, state.CurrentPage);
            Assert.Equal(1, state.NextPage);
            Assert.Null(state.TotalItemCount);
            Assert.False(state.MayHaveMore);
        }

        [Fact]
        public void Refresh_uses_the_latest_page_total_count()
        {
            CottonActivityFeedPagingState state = CottonActivityFeedPagingState.Empty
                .ApplyRefresh(CreatePage(page: 1, pageSize: 2, itemCount: 2, totalItemCount: 5));

            Assert.Equal(1, state.CurrentPage);
            Assert.Equal(2, state.NextPage);
            Assert.Equal(5, state.TotalItemCount);
            Assert.True(state.MayHaveMore);

            state = state.ApplyRefresh(CreatePage(page: 1, pageSize: 2, itemCount: 1, totalItemCount: null));

            Assert.Equal(1, state.CurrentPage);
            Assert.Null(state.TotalItemCount);
            Assert.False(state.MayHaveMore);
        }

        [Fact]
        public void Append_preserves_total_count_when_follow_up_headers_are_missing()
        {
            CottonActivityFeedPagingState state = CottonActivityFeedPagingState.Empty
                .ApplyRefresh(CreatePage(page: 1, pageSize: 2, itemCount: 2, totalItemCount: 5));

            state = state.ApplyAppend(CreatePage(page: 2, pageSize: 2, itemCount: 2, totalItemCount: null));

            Assert.Equal(2, state.CurrentPage);
            Assert.Equal(3, state.NextPage);
            Assert.Equal(5, state.TotalItemCount);
            Assert.True(state.MayHaveMore);

            state = state.ApplyAppend(CreatePage(page: 3, pageSize: 2, itemCount: 1, totalItemCount: null));

            Assert.Equal(3, state.CurrentPage);
            Assert.Equal(5, state.TotalItemCount);
            Assert.False(state.MayHaveMore);
        }

        [Fact]
        public void Append_requires_the_next_page()
        {
            CottonActivityFeedPagingState state = CottonActivityFeedPagingState.Empty
                .ApplyRefresh(CreatePage(page: 1, pageSize: 2, itemCount: 2, totalItemCount: 5));

            Assert.Throws<ArgumentException>(
                () => state.ApplyAppend(CreatePage(page: 3, pageSize: 2, itemCount: 1, totalItemCount: 5)));
        }

        private static CottonActivityFeedPageSnapshot CreatePage(
            int page,
            int pageSize,
            int itemCount,
            int? totalItemCount)
        {
            return new CottonActivityFeedPageSnapshot(
                new CottonActivityFeedQuery(page, pageSize),
                Enumerable.Range(0, itemCount).Select(CreateItem).ToArray(),
                totalItemCount);
        }

        private static CottonActivityFeedItemSnapshot CreateItem(int index)
        {
            byte[] bytes = new byte[16];
            bytes[0] = (byte)(index + 1);

            return new CottonActivityFeedItemSnapshot(
                new Guid(bytes),
                "Activity",
                null,
                CreatedAt.AddMinutes(-index),
                null,
                CottonActivityFeedPriority.Low,
                null);
        }
    }
}
