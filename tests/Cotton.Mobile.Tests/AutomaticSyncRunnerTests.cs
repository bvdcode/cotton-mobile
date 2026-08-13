using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncRunnerTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;

        public AutomaticSyncRunnerTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-automatic-sync", Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance,
                TimeProvider.System);
        }

        [Fact]
        public async Task MediaStoreTriggerRunsOnlyMediaRoots()
        {
            CottonSyncRootSnapshot folderRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot mediaRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [folderRoot, mediaRoot]);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                coordinator,
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            await runner.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.MediaStoreChanged);

            Assert.Equal([mediaRoot.Id], coordinator.RootIds);
        }

        [Fact]
        public async Task PeriodicTriggerRunsEveryRoot()
        {
            CottonSyncRootSnapshot folderRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot mediaRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [folderRoot, mediaRoot]);
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                coordinator,
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            await runner.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.PeriodicReconciliation);

            Assert.Equal(2, coordinator.RunRootCount);
            Assert.Contains(folderRoot.Id, coordinator.RootIds);
            Assert.Contains(mediaRoot.Id, coordinator.RootIds);
        }

        [Fact]
        public async Task FailureDoesNotPreventOtherRootsAndIsReportedForRetry()
        {
            CottonSyncRootSnapshot failingRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootSnapshot succeedingRoot = SyncTestRootFactory.CreateMediaStoreRoot();
            await _rootStore.SaveAsync(SyncTestRootFactory.InstanceUri, [failingRoot, succeedingRoot]);
            RecordingDeviceToCloudSyncCoordinator coordinator = new()
            {
                FailingRootId = failingRoot.Id,
            };
            CottonAutomaticSyncRunner runner = new(
                _rootStore,
                coordinator,
                NullLogger<CottonAutomaticSyncRunner>.Instance);

            await Assert.ThrowsAsync<AggregateException>(() => runner.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.PeriodicReconciliation));

            Assert.Equal(2, coordinator.RunRootCount);
            Assert.Contains(succeedingRoot.Id, coordinator.RootIds);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
