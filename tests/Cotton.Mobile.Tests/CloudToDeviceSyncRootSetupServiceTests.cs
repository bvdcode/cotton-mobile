using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncRootSetupServiceTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid OtherFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonCloudToDeviceSyncRootSetupService _service;

        public CloudToDeviceSyncRootSetupServiceTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-cloud-to-device-root-setup-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(new FixedSyncRootMetadataPathProvider(_directory));
            _service = new CottonCloudToDeviceSyncRootSetupService(_rootStore);
        }

        [Fact]
        public async Task Enable_app_private_root_creates_ready_cloud_to_device_root()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");

            CottonCloudToDeviceSyncRootSetupResult result =
                await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", folder);

            Assert.True(result.Created);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.CloudToDevice, result.Root.Direction);
            Assert.Equal(CottonSyncRootStorageKind.AppPrivateDirectory, result.Root.LocalRoot.StorageKind);
            Assert.Equal("account-1", result.Root.AccountScopeKey);
            Assert.Equal(FolderId, result.Root.CloudFolder.FolderId);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(result.Root.Id, saved.Id);
            Assert.Equal(result.Root.StableKey, saved.StableKey);
        }

        [Fact]
        public async Task Enable_app_private_root_is_idempotent_for_same_account_and_folder()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");

            CottonCloudToDeviceSyncRootSetupResult first =
                await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", folder);
            CottonCloudToDeviceSyncRootSetupResult second =
                await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", folder);

            Assert.True(first.Created);
            Assert.True(second.AlreadyConfigured);
            Assert.Equal(first.Root.Id, second.Root.Id);
            Assert.Equal(first.Root.StableKey, second.Root.StableKey);
            Assert.Single(await _rootStore.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Enable_app_private_root_updates_existing_not_ready_root_without_changing_id()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncRootSnapshot disabledRoot = CreateRoot(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                folder,
                CottonSyncRootPermissionStatus.Unavailable);
            await _rootStore.SaveAsync(InstanceUri, [disabledRoot]);

            CottonCloudToDeviceSyncRootSetupResult result =
                await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", folder);

            Assert.True(result.Updated);
            Assert.Equal(disabledRoot.Id, result.Root.Id);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncRootPermissionStatus.Available, result.Root.LocalRoot.PermissionStatus);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(disabledRoot.Id, saved.Id);
            Assert.True(saved.CanRunSync);
        }

        [Fact]
        public async Task Enable_app_private_root_allows_different_folders_for_same_account()
        {
            await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", CreateFolder(FolderId, "Projects"));
            await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", CreateFolder(OtherFolderId, "Archive"));

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(InstanceUri);

            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == FolderId);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == OtherFolderId);
        }

        [Fact]
        public async Task Enable_app_private_root_allows_same_folder_for_different_accounts()
        {
            await _service.EnableAppPrivateRootAsync(InstanceUri, "account-1", CreateFolder(FolderId, "Projects"));
            await _service.EnableAppPrivateRootAsync(InstanceUri, "account-2", CreateFolder(FolderId, "Projects"));

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(InstanceUri);

            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => root.AccountScopeKey == "account-1");
            Assert.Contains(roots, root => root.AccountScopeKey == "account-2");
            Assert.Equal(2, roots.Select(root => root.StableKey).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_creates_ready_cloud_to_device_root()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");

            CottonCloudToDeviceSyncRootSetupResult result =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);

            Assert.True(result.Created);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.CloudToDevice, result.Root.Direction);
            Assert.Equal(CottonSyncRootStorageKind.UserSelectedDocumentTree, result.Root.LocalRoot.StorageKind);
            Assert.Equal("content://tree/primary%3AProjects", result.Root.LocalRoot.RootKey);
            Assert.Equal("Projects", result.Root.LocalRoot.DisplayName);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(result.Root.Id, saved.Id);
            Assert.Equal(result.Root.StableKey, saved.StableKey);
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_is_idempotent_for_same_grant()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");

            CottonCloudToDeviceSyncRootSetupResult first =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);
            CottonCloudToDeviceSyncRootSetupResult second =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);

            Assert.True(first.Created);
            Assert.True(second.AlreadyConfigured);
            Assert.Equal(first.Root.Id, second.Root.Id);
            Assert.Single(await _rootStore.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_rejects_missing_grant()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/primary%3AProjects",
                "Projects",
                CottonSyncRootPermissionStatus.NeedsUserGrant);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonUploadDestinationSnapshot CreateFolder(Guid folderId, string folderName)
        {
            return new CottonUploadDestinationSnapshot(folderId, folderName, $"Files / {folderName}");
        }

        private static CottonSyncLocalRootSnapshot CreateDocumentTreeRoot(string rootKey, string displayName)
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                rootKey,
                displayName,
                CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            CottonUploadDestinationSnapshot folder,
            CottonSyncRootPermissionStatus permissionStatus)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "account-1",
                folder,
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-cloud-to-device",
                    "On this device",
                    permissionStatus),
                CottonSyncDirection.CloudToDevice);
        }

        private class FixedSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
        {
            private readonly string _directory;

            public FixedSyncRootMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncRootMetadataDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }
    }
}
