using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TransferQueueContractsTests
    {
        private static readonly Guid TransferId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = new(2026, 6, 19, 10, 1, 0, DateTimeKind.Utc);

        [Fact]
        public void Upload_transfer_starts_as_cancelable_queued_work()
        {
            CottonTransferQueueItem item = CottonTransferQueueItem.CreateUpload(
                TransferId,
                " photo.jpg ",
                200,
                CreatedAt);

            Assert.Equal(TransferId, item.Id);
            Assert.Equal(CottonTransferKind.Upload, item.Kind);
            Assert.Equal("photo.jpg", item.DisplayName);
            Assert.Equal(CottonTransferStatus.Queued, item.Status);
            Assert.Equal(0, item.Progress.TransferredBytes);
            Assert.Equal(200, item.Progress.TotalBytes);
            Assert.Equal(0, item.AttemptCount);
            Assert.False(item.IsTerminal);
            Assert.True(item.CanCancel);
            Assert.False(item.CanRetry);
        }

        [Fact]
        public void Progress_reports_percent_for_known_size_and_bytes_for_unknown_size()
        {
            var known = new CottonTransferProgressSnapshot(125, 200);
            var unknown = new CottonTransferProgressSnapshot(1536, null);

            Assert.Equal(62, known.Percent);
            Assert.Equal("62%", known.DisplayText);
            Assert.Null(unknown.Percent);
            Assert.Equal("1.5 KB", unknown.DisplayText);
        }

        [Fact]
        public void Running_transfer_reports_progress_and_completes_at_total_size()
        {
            CottonTransferQueueItem item = CreateUpload(totalBytes: 200)
                .Start(Later)
                .ReportProgress(125, Later.AddSeconds(1))
                .Complete(Later.AddSeconds(2));

            Assert.Equal(CottonTransferStatus.Completed, item.Status);
            Assert.True(item.IsTerminal);
            Assert.False(item.CanCancel);
            Assert.False(item.CanRetry);
            Assert.Equal(200, item.Progress.TransferredBytes);
            Assert.Equal(100, item.Progress.Percent);
            Assert.Equal(1, item.AttemptCount);
        }

        [Fact]
        public void Failed_transfer_can_retry_without_incrementing_attempt_until_restart()
        {
            CottonTransferQueueItem failed = CreateUpload(totalBytes: 200)
                .Start(Later)
                .ReportProgress(50, Later.AddSeconds(1))
                .Fail("Offline", Later.AddSeconds(2));

            CottonTransferQueueItem queued = failed.Retry(Later.AddSeconds(3));
            CottonTransferQueueItem runningAgain = queued.Start(Later.AddSeconds(4));

            Assert.Equal(CottonTransferStatus.Failed, failed.Status);
            Assert.True(failed.CanRetry);
            Assert.Equal("Offline", failed.FailureMessage);
            Assert.Equal(1, failed.AttemptCount);

            Assert.Equal(CottonTransferStatus.Queued, queued.Status);
            Assert.Null(queued.FailureMessage);
            Assert.Equal(1, queued.AttemptCount);

            Assert.Equal(CottonTransferStatus.Running, runningAgain.Status);
            Assert.Equal(2, runningAgain.AttemptCount);
        }

        [Fact]
        public void Restart_restores_running_transfer_to_queued_without_touching_terminal_items()
        {
            CottonTransferQueueItem running = CreateUpload(totalBytes: 200)
                .Start(Later)
                .ReportProgress(50, Later.AddSeconds(1));
            CottonTransferQueueItem completed = running.Complete(Later.AddSeconds(2));

            CottonTransferQueueItem restoredRunning = running.RestoreAfterRestart(Later.AddMinutes(5));
            CottonTransferQueueItem restoredCompleted = completed.RestoreAfterRestart(Later.AddMinutes(5));

            Assert.Equal(CottonTransferStatus.Queued, restoredRunning.Status);
            Assert.Equal(50, restoredRunning.Progress.TransferredBytes);
            Assert.Equal(1, restoredRunning.AttemptCount);
            Assert.Equal(Later.AddMinutes(5), restoredRunning.UpdatedAtUtc);

            Assert.Same(completed, restoredCompleted);
        }

        [Fact]
        public void Cancel_stops_non_terminal_transfer_and_blocks_retry()
        {
            CottonTransferQueueItem cancelled = CreateUpload(totalBytes: null).Cancel(Later);

            Assert.Equal(CottonTransferStatus.Cancelled, cancelled.Status);
            Assert.True(cancelled.IsTerminal);
            Assert.False(cancelled.CanCancel);
            Assert.False(cancelled.CanRetry);
            Assert.Throws<InvalidOperationException>(() => cancelled.Start(Later.AddSeconds(1)));
        }

        [Fact]
        public void Mark_failed_can_fail_restored_non_terminal_transfer()
        {
            CottonTransferQueueItem queued = CreateUpload(totalBytes: 200);

            CottonTransferQueueItem failed = queued.MarkFailed("Missing staged file", Later);

            Assert.Equal(CottonTransferStatus.Failed, failed.Status);
            Assert.True(failed.CanRetry);
            Assert.Equal("Missing staged file", failed.FailureMessage);
            Assert.Equal(Later, failed.UpdatedAtUtc);

            CottonTransferQueueItem cancelled = queued.Cancel(Later.AddSeconds(1));

            Assert.Throws<InvalidOperationException>(() => cancelled.MarkFailed("Nope", Later.AddSeconds(2)));
        }

        [Fact]
        public void Invalid_transitions_are_rejected()
        {
            CottonTransferQueueItem queued = CreateUpload(totalBytes: 200);
            CottonTransferQueueItem running = queued.Start(Later);
            CottonTransferQueueItem completed = running.Complete(Later.AddSeconds(1));

            Assert.Throws<InvalidOperationException>(() => queued.ReportProgress(1, Later));
            Assert.Throws<ArgumentOutOfRangeException>(() => running.ReportProgress(201, Later));
            Assert.Throws<ArgumentOutOfRangeException>(() => running.ReportProgress(-1, Later));
            Assert.Throws<InvalidOperationException>(() => completed.Cancel(Later));
            Assert.Throws<InvalidOperationException>(() => completed.Retry(Later));
        }

        [Fact]
        public void Transfer_contracts_reject_invalid_identity_and_size()
        {
            Assert.Throws<ArgumentException>(() =>
                CottonTransferQueueItem.CreateUpload(Guid.Empty, "photo.jpg", 1, CreatedAt));
            Assert.Throws<ArgumentException>(() =>
                CottonTransferQueueItem.CreateUpload(TransferId, " ", 1, CreatedAt));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonTransferQueueItem.CreateUpload(TransferId, "photo.jpg", -1, CreatedAt));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonTransferQueueItem.Restore(
                    TransferId,
                    (CottonTransferKind)99,
                    "photo.jpg",
                    CottonTransferStatus.Queued,
                    0,
                    1,
                    0,
                    null,
                    CreatedAt,
                    CreatedAt));
        }

        private static CottonTransferQueueItem CreateUpload(long? totalBytes)
        {
            return CottonTransferQueueItem.CreateUpload(TransferId, "photo.jpg", totalBytes, CreatedAt);
        }
    }
}
