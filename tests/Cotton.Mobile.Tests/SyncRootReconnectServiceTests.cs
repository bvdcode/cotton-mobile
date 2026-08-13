using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootReconnectServiceTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonSyncRootReconnectService _service;

        public SyncRootReconnectServiceTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-sync-reconnect", Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance,
                TimeProvider.System);
            _service = new CottonSyncRootReconnectService(_rootStore);
        }

        [Fact]
        public async Task ReconnectDocumentTreeRestoresAccessAndPreservesConfiguration()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot(
                CottonSyncRootPermissionStatus.Revoked,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            CottonSyncLocalRootSnapshot replacement = new(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                root.LocalRoot.RootKey,
                root.LocalRoot.DisplayName,
                CottonSyncRootPermissionStatus.Available);

            CottonSyncRootSnapshot result = await _service.ReconnectAsync(root, replacement);

            Assert.Equal(root.Id, result.Id);
            Assert.Equal(root.UploadOriginalRetention, result.UploadOriginalRetention);
            Assert.True(result.CanRunSync);
        }

        [Fact]
        public async Task ReconnectMediaStoreRestoresPermission()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot(
                CottonSyncRootPermissionStatus.Revoked);
            CottonSyncLocalRootSnapshot replacement = new(
                CottonSyncRootStorageKind.MediaStore,
                root.LocalRoot.RootKey,
                root.LocalRoot.DisplayName,
                CottonSyncRootPermissionStatus.Available);

            CottonSyncRootSnapshot result = await _service.ReconnectAsync(root, replacement);

            Assert.True(result.CanRunSync);
            Assert.True(result.LocalRoot.UsesMediaStore);
        }

        [Fact]
        public async Task ReconnectRejectsDifferentSource()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot(
                CottonSyncRootPermissionStatus.Revoked);
            CottonSyncLocalRootSnapshot replacement = new(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/primary%3AOther",
                "Other",
                CottonSyncRootPermissionStatus.Available);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.ReconnectAsync(root, replacement));
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
