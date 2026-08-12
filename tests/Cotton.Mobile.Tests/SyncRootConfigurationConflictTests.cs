using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncRootConfigurationTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncRootConfigurationConflictTests : IDisposable
    {
        private static readonly Guid ArchiveFolderId =
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonSyncRootConfigurationService _service;

        public SyncRootConfigurationConflictTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-configuration-conflict-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance, TimeProvider.System);
            _service = new CottonSyncRootConfigurationService(_rootStore);
        }

        [Fact]
        public async Task ConfigureSameLocalRootForAnotherCloudFolderReportsConflict()
        {
            CottonSyncRootConfigurationResult existing = await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);

            CottonSyncRootConfigurationResult conflict =
                await _service.ConfigureUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    CreateFolder(ArchiveFolderId, "Archive"),
                    CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects"),
                    CottonSyncDirection.Bidirectional,
                    CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(conflict.AlreadyConfigured);
            Assert.Equal(existing.Root.Id, conflict.Root.Id);
            Assert.Single(await _rootStore.LoadAsync(InstanceUri));
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
