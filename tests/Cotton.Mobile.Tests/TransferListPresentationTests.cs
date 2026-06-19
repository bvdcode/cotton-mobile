using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TransferListPresentationTests
    {
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Transfer_activity_signal_notifies_subscribers()
        {
            var signal = new CottonTransferActivitySignal();
            int eventCount = 0;
            signal.TransferActivityChanged += (_, _) => eventCount++;

            signal.NotifyTransferActivityChanged();

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void Snapshot_reports_empty_transfer_state()
        {
            CottonTransferListSnapshot snapshot = CottonTransferListSnapshot.Create([]);

            Assert.True(snapshot.IsEmpty);
            Assert.Empty(snapshot.Items);
            Assert.Equal("0 transfers", snapshot.SummaryText);
            Assert.Equal("No transfers yet", snapshot.EmptyMessage);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.EmptyDetails));
        }

        [Fact]
        public void Snapshot_sorts_newest_transfers_first()
        {
            CottonTransferQueueItem oldTransfer = CreateUpload(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "old.jpg");
            CottonTransferQueueItem newTransfer = CreateUpload(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "new.jpg")
                .Start(CreatedAt.AddMinutes(5));

            CottonTransferListSnapshot snapshot = CottonTransferListSnapshot.Create([oldTransfer, newTransfer]);

            Assert.Equal("new.jpg", snapshot.Items[0].DisplayName);
            Assert.Equal("old.jpg", snapshot.Items[1].DisplayName);
            Assert.Equal("2 transfers", snapshot.SummaryText);
        }

        [Fact]
        public void Snapshot_formats_queued_running_failed_completed_and_cancelled_states()
        {
            CottonTransferQueueItem queued = CreateUpload(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "queued.jpg");
            CottonTransferQueueItem running = CreateUpload(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "running.jpg")
                .Start(CreatedAt.AddSeconds(1))
                .ReportProgress(50, CreatedAt.AddSeconds(2));
            CottonTransferQueueItem failed = CreateUpload(
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    "failed.jpg")
                .Start(CreatedAt.AddSeconds(3))
                .Fail("Offline", CreatedAt.AddSeconds(4));
            CottonTransferQueueItem completed = CreateUpload(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "done.jpg")
                .Start(CreatedAt.AddSeconds(5))
                .Complete(CreatedAt.AddSeconds(6));
            CottonTransferQueueItem cancelled = CreateUpload(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    "cancelled.jpg")
                .Cancel(CreatedAt.AddSeconds(7));

            CottonTransferListSnapshot snapshot = CottonTransferListSnapshot.Create(
                [queued, running, failed, completed, cancelled]);

            CottonTransferListItem queuedItem = Find(snapshot, "queued.jpg");
            Assert.Equal("Queued", queuedItem.StatusText);
            Assert.Equal("Waiting", queuedItem.ProgressText);
            Assert.True(queuedItem.IsProgressVisible);

            CottonTransferListItem runningItem = Find(snapshot, "running.jpg");
            Assert.Equal("Uploading", runningItem.StatusText);
            Assert.Equal("50% · 50 B of 100 B", runningItem.ProgressText);
            Assert.Equal(0.5d, runningItem.ProgressFraction);

            CottonTransferListItem failedItem = Find(snapshot, "failed.jpg");
            Assert.Equal("Failed", failedItem.StatusText);
            Assert.True(failedItem.IsFailureVisible);
            Assert.Equal("Offline", failedItem.FailureMessage);

            CottonTransferListItem completedItem = Find(snapshot, "done.jpg");
            Assert.Equal("Completed", completedItem.StatusText);
            Assert.False(completedItem.IsProgressVisible);

            CottonTransferListItem cancelledItem = Find(snapshot, "cancelled.jpg");
            Assert.Equal("Cancelled", cancelledItem.StatusText);
            Assert.False(cancelledItem.IsProgressVisible);
        }

        [Fact]
        public void Snapshot_includes_destination_in_transfer_detail()
        {
            CottonTransferQueueItem transfer = CottonTransferQueueItem.CreateUpload(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "photo.jpg",
                100,
                CreatedAt,
                new CottonTransferDestinationSnapshot(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Camera Uploads",
                    "Files / Camera Uploads"));

            CottonTransferListItem item =
                Assert.Single(CottonTransferListSnapshot.Create([transfer]).Items);

            Assert.Equal("Upload waiting to Files / Camera Uploads", item.DetailText);
        }

        [Fact]
        public void Activity_indicator_hides_completed_and_cancelled_transfers()
        {
            CottonTransferQueueItem completed = CreateUpload(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "done.jpg")
                .Start(CreatedAt.AddSeconds(1))
                .Complete(CreatedAt.AddSeconds(2));
            CottonTransferQueueItem cancelled = CreateUpload(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "cancelled.jpg")
                .Cancel(CreatedAt.AddSeconds(3));

            CottonTransferActivityIndicator indicator = CottonTransferActivityIndicator.Create([completed, cancelled]);

            Assert.False(indicator.IsVisible);
            Assert.Equal(0, indicator.ActiveCount);
            Assert.Equal(string.Empty, indicator.Text);
        }

        [Fact]
        public void Activity_indicator_prioritizes_failed_then_running_then_waiting_state()
        {
            CottonTransferQueueItem queued = CreateUpload(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "queued.jpg");
            CottonTransferQueueItem running = CreateUpload(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "running.jpg")
                .Start(CreatedAt.AddSeconds(1));
            CottonTransferQueueItem failed = CreateUpload(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "failed.jpg")
                .Start(CreatedAt.AddSeconds(2))
                .Fail("Offline", CreatedAt.AddSeconds(3));

            CottonTransferActivityIndicator failedIndicator =
                CottonTransferActivityIndicator.Create([queued, running, failed]);
            CottonTransferActivityIndicator runningIndicator =
                CottonTransferActivityIndicator.Create([queued, running]);
            CottonTransferActivityIndicator waitingIndicator =
                CottonTransferActivityIndicator.Create([queued]);

            Assert.True(failedIndicator.IsVisible);
            Assert.True(failedIndicator.HasFailures);
            Assert.Equal("1 failed transfer", failedIndicator.Text);
            Assert.Equal("1 running", failedIndicator.Details);

            Assert.Equal("Uploading 1 item", runningIndicator.Text);
            Assert.Equal("1 running, 1 queued", runningIndicator.Details);

            Assert.Equal("1 transfer waiting", waitingIndicator.Text);
            Assert.Equal("Tap for details", waitingIndicator.Details);
        }

        [Fact]
        public void Activity_indicator_surfaces_waiting_work_next_to_failed_work()
        {
            CottonTransferQueueItem queued = CreateUpload(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "queued.jpg");
            CottonTransferQueueItem failed = CreateUpload(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "failed.jpg")
                .Start(CreatedAt.AddSeconds(1))
                .Fail("Offline", CreatedAt.AddSeconds(2));

            CottonTransferActivityIndicator indicator = CottonTransferActivityIndicator.Create([queued, failed]);

            Assert.Equal(2, indicator.ActiveCount);
            Assert.Equal("1 failed transfer", indicator.Text);
            Assert.Equal("1 waiting", indicator.Details);
        }

        private static CottonTransferListItem Find(
            CottonTransferListSnapshot snapshot,
            string displayName,
            string? statusText = null)
        {
            return snapshot.Items.Single(item =>
                item.DisplayName == displayName
                && (statusText is null || item.StatusText == statusText));
        }

        private static CottonTransferQueueItem CreateUpload(Guid id, string displayName)
        {
            return CottonTransferQueueItem.CreateUpload(id, displayName, 100, CreatedAt);
        }
    }
}
