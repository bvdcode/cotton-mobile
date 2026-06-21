using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationLaunchStateTests
    {
        [Fact]
        public void NotifyNotificationOpened_records_pending_launch_and_raises_event()
        {
            var state = new CottonNotificationLaunchState();
            var request = new CottonNotificationLaunchRequest(
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
                CottonRemotePushEventCategory.SecuritySession);
            int eventCount = 0;
            state.NotificationLaunchRequested += (_, _) => eventCount++;

            state.NotifyNotificationOpened(request);

            Assert.Equal(1, eventCount);
            Assert.Equal(1, state.PendingNotificationLaunchCount);
        }

        [Fact]
        public void TryConsumePendingNotificationLaunch_consumes_requests_in_order()
        {
            var state = new CottonNotificationLaunchState();
            var first = new CottonNotificationLaunchRequest(
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
                CottonRemotePushEventCategory.SharedFile);
            var second = new CottonNotificationLaunchRequest(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                CottonRemotePushEventCategory.SecuritySession);
            state.NotifyNotificationOpened(first);
            state.NotifyNotificationOpened(second);

            CottonNotificationLaunchRequest? consumedFirst = state.TryConsumePendingNotificationLaunch();

            Assert.Same(first, consumedFirst);
            Assert.Equal(1, state.PendingNotificationLaunchCount);
            CottonNotificationLaunchRequest? consumedSecond = state.TryConsumePendingNotificationLaunch();

            Assert.Same(second, consumedSecond);
            Assert.Equal(0, state.PendingNotificationLaunchCount);
            Assert.Null(state.TryConsumePendingNotificationLaunch());
        }

        [Fact]
        public void Notification_launch_request_requires_valid_payload()
        {
            Assert.Throws<ArgumentException>(
                () => new CottonNotificationLaunchRequest(
                    Guid.Empty,
                    CottonRemotePushEventCategory.SecuritySession));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonNotificationLaunchRequest(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    CottonRemotePushEventCategory.AccessRequest));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonNotificationLaunchRequest(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    CottonRemotePushEventCategory.CommentMention));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonNotificationLaunchRequest(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    (CottonRemotePushEventCategory)42));
        }

        [Fact]
        public void TryCreateNotificationLaunchRequest_ignores_empty_or_unsupported_payload()
        {
            Guid notificationId = Guid.Parse("11111111-2222-3333-4444-555555555555");

            Assert.Null(CottonNotificationLaunchRequest.TryCreate(
                Guid.Empty,
                CottonRemotePushEventCategory.SharedFile));
            Assert.Null(CottonNotificationLaunchRequest.TryCreate(
                notificationId,
                CottonRemotePushEventCategory.AccessRequest));
            Assert.Null(CottonNotificationLaunchRequest.TryCreate(
                notificationId,
                CottonRemotePushEventCategory.CommentMention));
            Assert.Null(CottonNotificationLaunchRequest.TryCreate(
                notificationId,
                (CottonRemotePushEventCategory)42));

            CottonNotificationLaunchRequest? request =
                CottonNotificationLaunchRequest.TryCreate(
                    notificationId,
                    CottonRemotePushEventCategory.SecuritySession);

            Assert.NotNull(request);
            Assert.Equal(notificationId, request.NotificationId);
            Assert.Equal(CottonRemotePushEventCategory.SecuritySession, request.Category);
        }

        [Fact]
        public void ClearPendingNotificationLaunches_removes_all_pending_requests()
        {
            var state = new CottonNotificationLaunchState();
            state.NotifyNotificationOpened(
                new CottonNotificationLaunchRequest(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    CottonRemotePushEventCategory.SharedFile));
            state.NotifyNotificationOpened(
                new CottonNotificationLaunchRequest(
                    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    CottonRemotePushEventCategory.SecuritySession));

            state.ClearPendingNotificationLaunches();

            Assert.Equal(0, state.PendingNotificationLaunchCount);
            Assert.Null(state.TryConsumePendingNotificationLaunch());
        }
    }
}
