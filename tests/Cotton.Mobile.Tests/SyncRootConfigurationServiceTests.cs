using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncRootConfigurationTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncRootConfigurationServiceTests : IDisposable
    {
        private static readonly Guid ExistingRootId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid OtherFolderId =
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonSyncRootConfigurationService _service;

        public SyncRootConfigurationServiceTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-configuration-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(new FixedSyncRootMetadataPathProvider(_directory));
            _service = new CottonSyncRootConfigurationService(_rootStore);
        }

        [Theory]
        [InlineData(CottonUploadOriginalRetention.KeepOriginals)]
        [InlineData(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload)]
        public async Task Configure_device_to_cloud_root_creates_ready_root_with_requested_retention(
            CottonUploadOriginalRetention retention)
        {
            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.DeviceToCloud,
                retention);

            Assert.True(result.Created);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.DeviceToCloud, result.Root.Direction);
            Assert.Equal(retention, result.Root.UploadOriginalRetention);
            Assert.Equal(CottonSyncRootStorageKind.UserSelectedDocumentTree, result.Root.LocalRoot.StorageKind);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(result.Root.Id, saved.Id);
            Assert.Equal(retention, saved.UploadOriginalRetention);
        }

        [Fact]
        public async Task Configure_bidirectional_root_creates_ready_keep_originals_root()
        {
            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.Bidirectional,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(result.Created);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.Bidirectional, result.Root.Direction);
            Assert.Equal(CottonUploadOriginalRetention.KeepOriginals, result.Root.UploadOriginalRetention);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(result.Root.Id, saved.Id);
        }

        [Theory]
        [InlineData(CottonSyncDirection.DeviceToCloud, CottonUploadOriginalRetention.KeepOriginals)]
        [InlineData(CottonSyncDirection.DeviceToCloud, CottonUploadOriginalRetention.DeleteAfterConfirmedUpload)]
        [InlineData(CottonSyncDirection.Bidirectional, CottonUploadOriginalRetention.KeepOriginals)]
        public async Task Configure_same_ready_configuration_is_idempotent(
            CottonSyncDirection direction,
            CottonUploadOriginalRetention retention)
        {
            CottonSyncRootConfigurationResult first = await _service.ConfigureDefaultRootAsync(direction, retention);
            CottonSyncRootConfigurationResult second = await _service.ConfigureDefaultRootAsync(direction, retention);

            Assert.True(first.Created);
            Assert.True(second.AlreadyConfigured);
            Assert.Equal(first.Root.Id, second.Root.Id);
            Assert.Single(await _rootStore.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Configure_same_direction_updates_retention_without_changing_id()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot(
                "content://tree/primary%3AProjects",
                "Projects");
            CottonSyncRootSnapshot existingRoot = CreateRoot(
                InstanceUri,
                ExistingRootId,
                folder,
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);
            await _rootStore.SaveAsync(InstanceUri, [existingRoot]);

            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);

            Assert.True(result.Updated);
            Assert.Equal(existingRoot.Id, result.Root.Id);
            Assert.Equal(
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload,
                result.Root.UploadOriginalRetention);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(existingRoot.Id, saved.Id);
            Assert.Equal(
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload,
                saved.UploadOriginalRetention);
        }

        [Theory]
        [InlineData(CottonSyncDirection.DeviceToCloud)]
        [InlineData(CottonSyncDirection.Bidirectional)]
        public async Task Configure_same_direction_updates_permission_without_changing_id(
            CottonSyncDirection direction)
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncRootSnapshot existingRoot = CreateRoot(
                InstanceUri,
                ExistingRootId,
                folder,
                CreateDocumentTreeRoot(
                    "content://tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Revoked),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
            await _rootStore.SaveAsync(InstanceUri, [existingRoot]);

            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                direction,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(result.Updated);
            Assert.Equal(existingRoot.Id, result.Root.Id);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncRootPermissionStatus.Available, result.Root.LocalRoot.PermissionStatus);
        }

        [Fact]
        public async Task Configure_same_direction_updates_effective_names_without_changing_id()
        {
            CottonSyncRootSnapshot existingRoot = CreateRoot(
                InstanceUri,
                ExistingRootId,
                CreateFolder(FolderId, "Old cloud name"),
                CreateDocumentTreeRoot(
                    "content://tree/primary%3AProjects",
                    "Old local name"),
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);
            await _rootStore.SaveAsync(InstanceUri, [existingRoot]);

            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(result.Updated);
            Assert.Equal(existingRoot.Id, result.Root.Id);
            Assert.Equal("Projects", result.Root.CloudFolder.FolderName);
            Assert.Equal("Projects", result.Root.LocalRoot.DisplayName);
        }

        [Theory]
        [InlineData(CottonSyncDirection.DeviceToCloud, CottonSyncDirection.Bidirectional)]
        [InlineData(CottonSyncDirection.Bidirectional, CottonSyncDirection.DeviceToCloud)]
        [InlineData(CottonSyncDirection.CloudToDevice, CottonSyncDirection.DeviceToCloud)]
        public async Task Configure_existing_different_direction_reports_already_configured_without_switching(
            CottonSyncDirection existingDirection,
            CottonSyncDirection requestedDirection)
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot(
                "content://tree/primary%3AProjects",
                "Projects");
            CottonSyncRootSnapshot existingRoot = CreateRoot(
                InstanceUri,
                ExistingRootId,
                folder,
                localRoot,
                existingDirection,
                CottonUploadOriginalRetention.KeepOriginals);
            await _rootStore.SaveAsync(InstanceUri, [existingRoot]);

            CottonSyncRootConfigurationResult result = await _service.ConfigureDefaultRootAsync(
                requestedDirection,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.True(result.AlreadyConfigured);
            Assert.Equal(existingRoot.Id, result.Root.Id);
            Assert.Equal(existingDirection, result.Root.Direction);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(existingDirection, saved.Direction);
            Assert.Equal(existingRoot.Id, saved.Id);
        }

        [Fact]
        public async Task Configure_different_stable_key_creates_another_root()
        {
            await _service.ConfigureDefaultRootAsync(
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);
            CottonSyncRootConfigurationResult other =
                await _service.ConfigureUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    CreateFolder(OtherFolderId, "Archive"),
                    CreateDocumentTreeRoot("content://tree/primary%3AArchive", "Archive"),
                    CottonSyncDirection.Bidirectional,
                    CottonUploadOriginalRetention.KeepOriginals);

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(InstanceUri);

            Assert.True(other.Created);
            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == FolderId);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == OtherFolderId);
        }

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice)]
        [InlineData((CottonSyncDirection)999)]
        public async Task Configure_rejects_unsupported_direction(CottonSyncDirection direction)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _service.ConfigureDefaultRootAsync(direction, CottonUploadOriginalRetention.KeepOriginals));
        }

        [Fact]
        public async Task Configure_rejects_delete_retention_for_bidirectional_root()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ConfigureDefaultRootAsync(
                    CottonSyncDirection.Bidirectional,
                    CottonUploadOriginalRetention.DeleteAfterConfirmedUpload));
        }

        [Fact]
        public async Task Configure_rejects_undefined_retention()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _service.ConfigureDefaultRootAsync(
                    CottonSyncDirection.DeviceToCloud,
                    (CottonUploadOriginalRetention)999));
        }

        [Fact]
        public async Task Configure_rejects_missing_grant()
        {
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot(
                "content://tree/primary%3AProjects",
                "Projects",
                CottonSyncRootPermissionStatus.NeedsUserGrant);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ConfigureUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    CreateFolder(FolderId, "Projects"),
                    localRoot,
                    CottonSyncDirection.DeviceToCloud,
                    CottonUploadOriginalRetention.KeepOriginals));
        }

        [Fact]
        public async Task Configure_rejects_app_private_root()
        {
            CottonSyncLocalRootSnapshot localRoot = new(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-sync-root",
                "On this device",
                CottonSyncRootPermissionStatus.Available);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ConfigureUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    CreateFolder(FolderId, "Projects"),
                    localRoot,
                    CottonSyncDirection.DeviceToCloud,
                    CottonUploadOriginalRetention.KeepOriginals));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

    }
}
