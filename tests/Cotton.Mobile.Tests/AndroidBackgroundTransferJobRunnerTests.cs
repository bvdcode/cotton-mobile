using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidBackgroundTransferJobRunnerTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid TransferId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 22, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Run_restores_stale_running_transfer_before_exact_execution_retry()
        {
            CottonTransferQueueItem running = CottonTransferQueueItem
                .CreateUpload(TransferId, "upload.bin", 100, CreatedAt)
                .Start(CreatedAt.AddSeconds(1));
            CottonTransferQueueItem completed = running
                .RestoreAfterRestart(CreatedAt.AddMinutes(1))
                .Start(CreatedAt.AddMinutes(1).AddSeconds(1))
                .Complete(CreatedAt.AddMinutes(2));
            var executor = new FakeQueuedUploadExecutor(
                [
                    new CottonQueuedUploadExecutionResult(
                        CottonQueuedUploadExecutionStatus.TransferNotQueued,
                        running,
                        "Upload transfer is not waiting to run."),
                    new CottonQueuedUploadExecutionResult(
                        CottonQueuedUploadExecutionStatus.Completed,
                        completed,
                        null),
                ]);
            var restoreCoordinator = new FakeRestoreCoordinator([running.RestoreAfterRestart(CreatedAt.AddMinutes(1))]);
            var runner = new CottonAndroidBackgroundTransferJobRunner(executor, restoreCoordinator);

            CottonQueuedUploadExecutionResult result = await runner.RunAsync(InstanceUri, TransferId);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(CottonTransferStatus.Completed, result.Transfer?.Status);
            Assert.Equal(2, executor.ExecuteCalls.Count);
            Assert.All(executor.ExecuteCalls, call => Assert.Equal(TransferId, call.TransferId));
            Assert.Equal(InstanceUri, restoreCoordinator.RestoreCalls.Single());
        }

        [Fact]
        public async Task Run_does_not_restore_when_exact_execution_reaches_terminal_transfer()
        {
            CottonTransferQueueItem completed = CottonTransferQueueItem
                .CreateUpload(TransferId, "upload.bin", 100, CreatedAt)
                .Start(CreatedAt.AddSeconds(1))
                .Complete(CreatedAt.AddSeconds(2));
            var executor = new FakeQueuedUploadExecutor(
                [
                    new CottonQueuedUploadExecutionResult(
                        CottonQueuedUploadExecutionStatus.TransferNotQueued,
                        completed,
                        "Upload transfer is not waiting to run."),
                ]);
            var restoreCoordinator = new FakeRestoreCoordinator([]);
            var runner = new CottonAndroidBackgroundTransferJobRunner(executor, restoreCoordinator);

            CottonQueuedUploadExecutionResult result = await runner.RunAsync(InstanceUri, TransferId);

            Assert.Equal(CottonQueuedUploadExecutionStatus.TransferNotQueued, result.Status);
            Assert.Equal(CottonTransferStatus.Completed, result.Transfer?.Status);
            Assert.Single(executor.ExecuteCalls);
            Assert.Empty(restoreCoordinator.RestoreCalls);
        }

        private sealed class FakeQueuedUploadExecutor : ICottonQueuedUploadExecutor
        {
            private readonly Queue<CottonQueuedUploadExecutionResult> _results;

            public FakeQueuedUploadExecutor(IEnumerable<CottonQueuedUploadExecutionResult> results)
            {
                _results = new Queue<CottonQueuedUploadExecutionResult>(results);
            }

            public IReadOnlyList<(Uri InstanceUri, Guid TransferId)> ExecuteCalls { get; private set; } = [];

            public Task<CottonQueuedUploadExecutionResult> ExecuteNextAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CottonQueuedUploadExecutionResult> ExecuteAsync(
                Uri instanceUri,
                Guid transferId,
                CancellationToken cancellationToken = default)
            {
                ExecuteCalls = ExecuteCalls.Concat([(instanceUri, transferId)]).ToList();
                return Task.FromResult(_results.Dequeue());
            }
        }

        private sealed class FakeRestoreCoordinator : ICottonTransferQueueRestoreCoordinator
        {
            private readonly IReadOnlyList<CottonTransferQueueItem> _restoredItems;

            public FakeRestoreCoordinator(IReadOnlyList<CottonTransferQueueItem> restoredItems)
            {
                _restoredItems = restoredItems;
            }

            public IReadOnlyList<Uri> RestoreCalls { get; private set; } = [];

            public Task<IReadOnlyList<CottonTransferQueueItem>> RestoreAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                RestoreCalls = RestoreCalls.Concat([instanceUri]).ToList();
                return Task.FromResult(_restoredItems);
            }
        }
    }
}
