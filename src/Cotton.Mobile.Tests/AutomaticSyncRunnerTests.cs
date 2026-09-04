using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncRunnerTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonAutomaticSyncStatusStore _statusStore;
        private readonly FixedTimeProvider _timeProvider;

        public AutomaticSyncRunnerTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-automatic-sync", Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance,
                TimeProvider.System);
            _timeProvider = new FixedTimeProvider(new DateTime(2026, 8, 14, 18, 0, 0, DateTimeKind.Utc));
            _statusStore = new FileSystemCottonAutomaticSyncStatusStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonAutomaticSyncStatusStore>.Instance,
                _timeProvider);
        }

        [Fact]
        public async Task MediaStoreTriggerRunsOnlyMediaRoots()
        {
            CottonSyncRootSnapshot folderRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot mediaRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [folderRoot, mediaRoot], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);

            Assert.Equal([mediaRoot.Id], coordinator.RootIds);
        }

        [Fact]
        public async Task PeriodicTriggerRunsEveryRoot()
        {
            CottonSyncRootSnapshot folderRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot mediaRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [folderRoot, mediaRoot], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);

            Assert.Equal(2, coordinator.RunRootCount);
            Assert.Contains(folderRoot.Id, coordinator.RootIds);
            Assert.Contains(mediaRoot.Id, coordinator.RootIds);
        }

        [Fact]
        public async Task PeriodicTriggerRunsOnlyCurrentAccountRoots()
        {
            CottonSyncRootSnapshot currentRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot otherAccountRoot = SyncTestRootFactory.CreateMediaStoreRoot(
                accountScopeKey: "account-2");
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [currentRoot, otherAccountRoot], TestContext.Current.CancellationToken);
            await _statusStore.UpdateAsync(SyncTestRootFactory.InstanceUri, new HashSet<Guid> { currentRoot.Id, otherAccountRoot.Id }, [CottonAutomaticSyncRootStatusSnapshot.Failed(
                    otherAccountRoot.Id,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    CottonAutomaticSyncFailureKind.AuthenticationRequired)], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            CottonAutomaticSyncRunResult result = await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);

            Assert.Equal([currentRoot.Id], coordinator.RootIds);
            Assert.Equal([currentRoot.Id], result.SucceededRootIds);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _statusStore.LoadAsync(SyncTestRootFactory.InstanceUri, TestContext.Current.CancellationToken);
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, statuses[currentRoot.Id].Outcome);
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, statuses[otherAccountRoot.Id].Outcome);
        }

        [Fact]
        public async Task FailureDoesNotPreventOtherRootsAndIsReportedForRetry()
        {
            CottonSyncRootSnapshot failingRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot succeedingRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [failingRoot, succeedingRoot], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new()
            {
                FailingRootId = failingRoot.Id,
            };
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            CottonAutomaticSyncRunResult result = await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);

            Assert.Equal(2, coordinator.RunRootCount);
            Assert.Contains(succeedingRoot.Id, coordinator.RootIds);
            Assert.Equal([succeedingRoot.Id], result.SucceededRootIds);
            Assert.Equal([failingRoot.Id], result.FailedRootIds);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _statusStore.LoadAsync(SyncTestRootFactory.InstanceUri, TestContext.Current.CancellationToken);
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, statuses[failingRoot.Id].Outcome);
            Assert.Equal(CottonAutomaticSyncFailureKind.LocalReadFailed, statuses[failingRoot.Id].FailureKind);
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, statuses[succeedingRoot.Id].Outcome);
        }

        [Fact]
        public async Task SelectedRootRunDoesNotRepeatOtherRoots()
        {
            CottonSyncRootSnapshot selectedRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot otherRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [selectedRoot, otherRoot], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            CottonAutomaticSyncRunResult result = await runner.RunRootsAsync(SyncTestRootFactory.SessionScope, [selectedRoot.Id], TestContext.Current.CancellationToken);

            Assert.Equal([selectedRoot.Id], coordinator.RootIds);
            Assert.Equal([selectedRoot.Id], result.SucceededRootIds);
            Assert.Empty(result.FailedRootIds);
        }

        [Fact]
        public async Task InternalTimeoutIsRecordedWithoutCancellingOtherRoots()
        {
            CottonSyncRootSnapshot timedOutRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot succeedingRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [timedOutRoot, succeedingRoot], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new()
            {
                FailingRootId = timedOutRoot.Id,
                FailureException = new OperationCanceledException("Simulated request timeout."),
            };
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            CottonAutomaticSyncRunResult result = await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);

            Assert.Equal([timedOutRoot.Id], result.FailedRootIds);
            Assert.Equal([succeedingRoot.Id], result.SucceededRootIds);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _statusStore.LoadAsync(SyncTestRootFactory.InstanceUri, TestContext.Current.CancellationToken);
            Assert.Equal(CottonAutomaticSyncFailureKind.TimedOut, statuses[timedOutRoot.Id].FailureKind);
        }

        [Fact]
        public async Task BlockedItemsRequireActionAndAreNotReportedAsSuccess()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [root], TestContext.Current.CancellationToken);
            RecordingDeviceToCloudSyncCoordinator coordinator = new()
            {
                BlockedRootId = root.Id,
            };
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                new SyncExecutionWorkflow(coordinator, _rootStore, _statusStore, _timeProvider,
                    NullLogger<SyncExecutionWorkflow>.Instance),
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            CottonAutomaticSyncRunResult result = await runner.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);

            Assert.Empty(result.SucceededRootIds);
            CottonAutomaticSyncFailure failure = Assert.Single(result.Failures);
            Assert.Equal(root.Id, failure.RootId);
            Assert.Equal(CottonAutomaticSyncFailureKind.ActionRequired, failure.Kind);
            Assert.Empty(result.RetryableRootIds);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _statusStore.LoadAsync(SyncTestRootFactory.InstanceUri, TestContext.Current.CancellationToken);
            Assert.Equal(CottonAutomaticSyncOutcome.Failed, statuses[root.Id].Outcome);
            Assert.Equal(CottonAutomaticSyncFailureKind.ActionRequired, statuses[root.Id].FailureKind);
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
