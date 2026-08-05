using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.CloudToDeviceSyncCoordinatorTestData;

namespace Cotton.Mobile.Tests
{
    public abstract class CloudToDeviceSyncCoordinatorTestContext : IDisposable
    {
        private readonly string _directory;

        private protected readonly FileSystemCottonSyncRootStore _rootStore;
        private protected readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private protected readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private protected readonly CloudToDeviceCoordinatorFolderContentSource _folderContentSource;
        private protected readonly CloudToDeviceCoordinatorFileOperator _fileOperator;
        private protected readonly CottonCloudToDeviceSyncCoordinator _coordinator;

        public CloudToDeviceSyncCoordinatorTestContext()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-cloud-to-device-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(Path.Combine(_directory, "manifest")));
            _folderContentSource = new CloudToDeviceCoordinatorFolderContentSource();
            _fileOperator = new CloudToDeviceCoordinatorFileOperator();
            CottonCloudToDeviceSyncPlanExecutor executor = new(
                _fileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonCloudToDeviceSyncCoordinator(
                _rootStore,
                _pauseStore,
                _manifestStore,
                _folderContentSource,
                executor);
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
