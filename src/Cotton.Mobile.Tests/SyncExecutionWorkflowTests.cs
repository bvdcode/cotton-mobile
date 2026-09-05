using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncExecutionWorkflowTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "cotton-sync-outcomes", Guid.NewGuid().ToString("N"));
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonAutomaticSyncStatusStore _statusStore;
        private readonly FixedTimeProvider _timeProvider = new(new DateTime(2026, 9, 4, 18, 0, 0, DateTimeKind.Utc));
        private readonly RecordingDeviceToCloudSyncCoordinator _coordinator = new();
        private readonly SyncExecutionWorkflow _workflow;

        public SyncExecutionWorkflowTests()
        {
            FixedSyncRootMetadataPathProvider paths = new(_directory);
            _rootStore = new(paths, NullLogger<FileSystemCottonSyncRootStore>.Instance, _timeProvider);
            _statusStore = new(paths, NullLogger<FileSystemCottonAutomaticSyncStatusStore>.Instance, _timeProvider);
            _workflow = new(_coordinator, _rootStore, _statusStore, _timeProvider,
                NullLogger<SyncExecutionWorkflow>.Instance);
        }

        [Fact]
        public async Task ManualRetryReplacesAutomaticFailureAndPreservesOtherRoots()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot otherRoot = SyncTestRootFactory.CreateMediaStoreRoot(accountScopeKey: "account-2");
            await SaveRootsAsync(root, otherRoot);
            await _statusStore.UpdateAsync(SyncTestRootFactory.InstanceUri, new HashSet<Guid> { root.Id, otherRoot.Id },
                [CreateFailure(root.Id), CreateFailure(otherRoot.Id)], TestContext.Current.CancellationToken);
            CottonSyncRootListItem displayedRoot = new(root, automaticStatus: CreateFailure(root.Id));
            _statusStore.StatusesChanged += (_, args) => displayedRoot.SetAutomaticStatus(args.Statuses[root.Id]);

            await _workflow.RunRootAsync(SyncTestRootFactory.InstanceUri, root, TestContext.Current.CancellationToken);

            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses = await LoadStatusesAsync();
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, statuses[root.Id].Outcome);
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, statuses[otherRoot.Id].Outcome);
            Assert.False(displayedRoot.CanShowFailureDetails);
            Assert.StartsWith("Last synced", displayedRoot.StatusText, StringComparison.Ordinal);
            Assert.Equal(1, _coordinator.RunRootCount);
        }

        [Fact]
        public async Task RunAllPublishesEachCompletedRoot()
        {
            CottonSyncRootSnapshot first = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot second = SyncTestRootFactory.CreateMediaStoreRoot();
            await SaveRootsAsync(first, second);
            List<int> completedCounts = [];
            _statusStore.StatusesChanged += (_, args) => completedCounts.Add(args.Statuses.Count);

            string status = await _workflow.RunAllAsync(SyncTestRootFactory.InstanceUri, [first, second],
                TestContext.Current.CancellationToken);

            Assert.Equal([1, 2], completedCounts);
            Assert.Equal(2, _coordinator.RunRootCount);
            Assert.Equal("Sync complete. Everything is up to date.", status);
        }

        [Fact]
        public async Task ManualFailureIsPersistedBeforeItIsReported()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            await SaveRootsAsync(root);
            _coordinator.FailingRootId = root.Id;
            _coordinator.FailureException = new HttpRequestException("Connection interrupted.");

            await Assert.ThrowsAsync<HttpRequestException>(() => _workflow.RunRootAsync(
                SyncTestRootFactory.InstanceUri, root, TestContext.Current.CancellationToken));

            CottonAutomaticSyncRootStatusSnapshot status = (await LoadStatusesAsync())[root.Id];
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, status.Outcome);
            Assert.Equal(CottonAutomaticSyncFailureKind.NetworkUnavailable, status.FailureKind);
        }

        [Fact]
        public async Task RunAllContinuesAfterOneRootFails()
        {
            CottonSyncRootSnapshot first = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot second = SyncTestRootFactory.CreateMediaStoreRoot();
            await SaveRootsAsync(first, second);
            _coordinator.FailingRootId = first.Id;

            string message = await _workflow.RunAllAsync(SyncTestRootFactory.InstanceUri, [first, second],
                TestContext.Current.CancellationToken);

            Assert.Equal([first.Id, second.Id], _coordinator.RootIds);
            Assert.Contains("1 folder failed", message, StringComparison.Ordinal);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses = await LoadStatusesAsync();
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, statuses[first.Id].Outcome);
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, statuses[second.Id].Outcome);
        }

        [Theory]
        [InlineData(CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged, CottonAutomaticSyncFailureKind.ActionRequired, true)]
        [InlineData(CottonDeviceToCloudSyncActionKind.UploadedLocalVersionChanged, CottonAutomaticSyncFailureKind.UploadedFileChanged, false)]
        public async Task ManualBlockedUploadRequiresCorrectAction(
            CottonDeviceToCloudSyncActionKind action,
            CottonAutomaticSyncFailureKind failureKind,
            bool canResolvePending)
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            await SaveRootsAsync(root);
            _coordinator.BlockedRootId = root.Id;
            _coordinator.BlockedAction = action;

            await _workflow.RunRootAsync(SyncTestRootFactory.InstanceUri, root, TestContext.Current.CancellationToken);

            CottonAutomaticSyncRootStatusSnapshot status = (await LoadStatusesAsync())[root.Id];
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, status.Outcome);
            Assert.Equal(failureKind, status.FailureKind);
            CottonSyncRootListItem displayedRoot = new(root, automaticStatus: status);
            Assert.Equal(canResolvePending, displayedRoot.CanResolvePendingUpload);
            Assert.True(displayedRoot.CanShowFailureDetails);
        }

        [Fact]
        public async Task PausedRunDoesNotReplaceTheLastOutcomeWithSuccess()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            await SaveRootsAsync(root);
            _coordinator.PausedRootId = root.Id;
            await _statusStore.UpdateAsync(SyncTestRootFactory.InstanceUri, new HashSet<Guid> { root.Id },
                [CreateFailure(root.Id)], TestContext.Current.CancellationToken);

            await _workflow.RunRootAsync(SyncTestRootFactory.InstanceUri, root, TestContext.Current.CancellationToken);

            Assert.Equal(CottonAutomaticSyncOutcome.Failed, (await LoadStatusesAsync())[root.Id].Outcome);
        }

        [Fact]
        public async Task CancelledRunDoesNotRecordFailure()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            await SaveRootsAsync(root);
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _workflow.RunRootAsync(
                SyncTestRootFactory.InstanceUri, root, cancellation.Token));

            Assert.Empty(await LoadStatusesAsync());
        }

        private Task SaveRootsAsync(params CottonSyncRootSnapshot[] roots)
        {
            return _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, roots, TestContext.Current.CancellationToken);
        }

        private Task<IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>> LoadStatusesAsync()
        {
            return _statusStore.LoadAsync(SyncTestRootFactory.InstanceUri, TestContext.Current.CancellationToken);
        }

        private CottonAutomaticSyncRootStatusSnapshot CreateFailure(Guid rootId)
        {
            return CottonAutomaticSyncRootStatusSnapshot.Failed(rootId,
                _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10), CottonAutomaticSyncFailureKind.NetworkUnavailable);
        }

        public void Dispose()
        {
            _statusStore.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
