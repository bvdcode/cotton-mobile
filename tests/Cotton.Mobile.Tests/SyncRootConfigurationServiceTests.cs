using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootConfigurationServiceTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonSyncRootConfigurationService _service;

        public SyncRootConfigurationServiceTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-sync-config", Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance,
                TimeProvider.System);
            _service = new CottonSyncRootConfigurationService(_rootStore);
        }

        [Theory]
        [InlineData(CottonUploadOriginalRetention.KeepOriginals)]
        [InlineData(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload)]
        public async Task ConfigureDocumentTreeCreatesUploadRoot(CottonUploadOriginalRetention retention)
        {
            CottonSyncRootConfigurationResult result = await ConfigureAsync(
                CreateDocumentTreeLocalRoot(),
                retention);

            Assert.True(result.Created);
            Assert.Equal(CottonSyncDirection.DeviceToCloud, result.Root.Direction);
            Assert.Equal(retention, result.Root.UploadOriginalRetention);
            Assert.True(result.Root.CanRunSync);
            Assert.Single(await _rootStore.LoadAsync(SyncTestRootFactory.InstanceUri));
        }

        [Fact]
        public async Task ConfigureMediaStoreCreatesKeepOriginalsUploadRoot()
        {
            CottonSyncRootConfigurationResult result = await ConfigureAsync(
                CreateMediaStoreLocalRoot(),
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(result.Created);
            Assert.True(result.Root.LocalRoot.UsesMediaStore);
            Assert.False(result.Root.DeletesOriginalsAfterUpload);
        }

        [Fact]
        public async Task ConfigureMediaStoreRejectsDeleteAfterUpload()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => ConfigureAsync(
                CreateMediaStoreLocalRoot(),
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload));
        }

        [Fact]
        public async Task ConfigureSameLocalSourceForAnotherCloudFolderReturnsExistingRoot()
        {
            CottonSyncRootConfigurationResult first = await ConfigureAsync(
                CreateDocumentTreeLocalRoot(),
                CottonUploadOriginalRetention.KeepOriginals);
            CottonSyncRootConfigurationResult second = await _service.ConfigureRootAsync(
                SyncTestRootFactory.InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Archive", "Files / Archive"),
                CreateDocumentTreeLocalRoot(),
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(second.AlreadyConfigured);
            Assert.Equal(first.Root.Id, second.Root.Id);
            Assert.Single(await _rootStore.LoadAsync(SyncTestRootFactory.InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        private Task<CottonSyncRootConfigurationResult> ConfigureAsync(
            CottonSyncLocalRootSnapshot localRoot,
            CottonUploadOriginalRetention retention)
        {
            return _service.ConfigureRootAsync(
                SyncTestRootFactory.InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Projects", "Files / Projects"),
                localRoot,
                retention);
        }

        private static CottonSyncLocalRootSnapshot CreateDocumentTreeLocalRoot()
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/primary%3AProjects",
                "Projects",
                CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncLocalRootSnapshot CreateMediaStoreLocalRoot()
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.MediaStore,
                "content://media/external/file",
                "Photos and videos",
                CottonSyncRootPermissionStatus.Available);
        }
    }
}
