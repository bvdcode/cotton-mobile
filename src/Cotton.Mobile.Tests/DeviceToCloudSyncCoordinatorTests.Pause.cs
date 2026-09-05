using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PausingAnUploadPreservesItsReceiptAndResumesTheSameOperation(bool transportThrowsIOException)
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await PreparePausedUploadAsync(root);
            _fileOperator.ReportCancellationAsIOException = transportThrowsIOException;
            Task<CottonDeviceToCloudSyncRunSummary> running = _coordinator.RunRootAsync(
                InstanceUri, root, TestContext.Current.CancellationToken);
            await _fileOperator.UploadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, true, TestContext.Current.CancellationToken);
            await _executionLock.CancelAsync(root);
            CottonDeviceToCloudSyncRunSummary paused = await running;

            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedPaused, Assert.Single(paused.RootResults).Status);
            Assert.Empty(_progressHub.GetCurrent());
            Assert.Empty(_fileOperator.UploadedItems);
            CottonUploadReceiptSnapshot pending = Assert.Single(await _uploadReceiptStore.LoadAsync(
                InstanceUri, root, TestContext.Current.CancellationToken));
            Assert.True(pending.IsPending);

            _fileOperator.UploadGate = null;
            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, false, TestContext.Current.CancellationToken);
            CottonDeviceToCloudSyncRunSummary resumed = await _coordinator.RunRootAsync(
                InstanceUri, root, TestContext.Current.CancellationToken);

            Assert.Equal(1, resumed.UploadedCount);
            Assert.Equal([pending.OperationId, pending.OperationId], _fileOperator.UploadOperationIds);
            Assert.True(Assert.Single(await _uploadReceiptStore.LoadAsync(
                InstanceUri, root, TestContext.Current.CancellationToken)).IsUploaded);
        }

        [Fact]
        public async Task AQueuedRunRechecksPauseAfterAcquiringTheRoot()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            TaskCompletionSource held = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<int> active = _executionLock.ExecuteAsync(root, async token =>
            {
                held.SetResult();
                await release.Task.WaitAsync(token);
                return 1;
            }, TestContext.Current.CancellationToken);
            await held.Task.WaitAsync(TestContext.Current.CancellationToken);
            Task<CottonDeviceToCloudSyncRunSummary> queued = _coordinator.RunRootAsync(
                InstanceUri, root, TestContext.Current.CancellationToken);

            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, true, TestContext.Current.CancellationToken);
            release.SetResult();
            await active;
            CottonDeviceToCloudSyncRunSummary result = await queued;

            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedPaused, Assert.Single(result.RootResults).Status);
            Assert.Empty(_localTreeReader.ReadRootIds);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExternalCancellationIsNotReportedAsPause(bool transportThrowsIOException)
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await PreparePausedUploadAsync(root);
            _fileOperator.ReportCancellationAsIOException = transportThrowsIOException;
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            Task<CottonDeviceToCloudSyncRunSummary> running = _coordinator.RunRootAsync(
                InstanceUri, root, cancellation.Token);
            await _fileOperator.UploadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
            Assert.Empty(_progressHub.GetCurrent());
        }

        [Fact]
        public async Task AnIOExceptionWithoutCancellationIsNotReportedAsPause()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await PreparePausedUploadAsync(root);
            _fileOperator.UploadGate = Task.FromException(new IOException("Connection interrupted."));

            await Assert.ThrowsAsync<IOException>(() => _coordinator.RunRootAsync(
                InstanceUri, root, TestContext.Current.CancellationToken));

            Assert.Empty(_progressHub.GetCurrent());
            Assert.True(Assert.Single(await _uploadReceiptStore.LoadAsync(
                InstanceUri, root, TestContext.Current.CancellationToken)).IsPending);
        }

        private async Task PreparePausedUploadAsync(CottonSyncRootSnapshot root)
        {
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id,
                CreateLocalContent(CreateLocalFile("photo.jpg", "photo.jpg", "document:photo")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.SetUploadResult("photo.jpg", FirstFileId, "\"etag-photo\"");
            _fileOperator.UploadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        }
    }
}
