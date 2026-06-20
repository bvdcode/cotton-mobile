using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncRootSetupServiceTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid OtherFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonBidirectionalSyncRootSetupService _service;

        public BidirectionalSyncRootSetupServiceTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-bidirectional-root-setup-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(new FixedSyncRootMetadataPathProvider(_directory));
            _service = new CottonBidirectionalSyncRootSetupService(_rootStore);
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_creates_ready_bidirectional_root()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");

            CottonBidirectionalSyncRootSetupResult result =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);

            Assert.True(result.Created);
            Assert.True(result.Enabled);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.Bidirectional, result.Root.Direction);
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

            CottonBidirectionalSyncRootSetupResult first =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);
            CottonBidirectionalSyncRootSetupResult second =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);

            Assert.True(first.Created);
            Assert.True(second.AlreadyConfigured);
            Assert.True(second.Enabled);
            Assert.Equal(first.Root.Id, second.Root.Id);
            Assert.Single(await _rootStore.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_updates_existing_not_ready_root_without_changing_id()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncRootSnapshot disabledRoot = CreateRoot(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                folder,
                CreateDocumentTreeRoot(
                    "content://tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Revoked),
                CottonSyncDirection.Bidirectional);
            await _rootStore.SaveAsync(InstanceUri, [disabledRoot]);

            CottonBidirectionalSyncRootSetupResult result =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects"));

            Assert.True(result.Updated);
            Assert.True(result.Enabled);
            Assert.Equal(disabledRoot.Id, result.Root.Id);
            Assert.True(result.Root.CanRunSync);
            Assert.Equal(CottonSyncDirection.Bidirectional, result.Root.Direction);
            Assert.Equal(CottonSyncRootPermissionStatus.Available, result.Root.LocalRoot.PermissionStatus);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(disabledRoot.Id, saved.Id);
            Assert.True(saved.CanRunSync);
        }

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice)]
        [InlineData(CottonSyncDirection.DeviceToCloud)]
        public async Task Enable_user_selected_document_tree_root_upgrades_existing_one_way_root_without_changing_id(
            CottonSyncDirection existingDirection)
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");
            CottonSyncRootSnapshot oneWayRoot = CreateRoot(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                folder,
                localRoot,
                existingDirection);
            await _rootStore.SaveAsync(InstanceUri, [oneWayRoot]);

            CottonBidirectionalSyncRootSetupResult result =
                await _service.EnableUserSelectedDocumentTreeRootAsync(
                    InstanceUri,
                    "account-1",
                    folder,
                    localRoot);

            Assert.True(result.Updated);
            Assert.True(result.Enabled);
            Assert.Equal(oneWayRoot.Id, result.Root.Id);
            Assert.Equal(CottonSyncDirection.Bidirectional, result.Root.Direction);
            Assert.Equal(oneWayRoot.StableKey, result.Root.StableKey);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(oneWayRoot.Id, saved.Id);
            Assert.Equal(CottonSyncDirection.Bidirectional, saved.Direction);
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_allows_different_folders_for_same_account()
        {
            CottonSyncLocalRootSnapshot projectsRoot =
                CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");
            CottonSyncLocalRootSnapshot archiveRoot =
                CreateDocumentTreeRoot("content://tree/primary%3AArchive", "Archive");

            await _service.EnableUserSelectedDocumentTreeRootAsync(
                InstanceUri,
                "account-1",
                CreateFolder(FolderId, "Projects"),
                projectsRoot);
            await _service.EnableUserSelectedDocumentTreeRootAsync(
                InstanceUri,
                "account-1",
                CreateFolder(OtherFolderId, "Archive"),
                archiveRoot);

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(InstanceUri);

            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == FolderId);
            Assert.Contains(roots, root => root.CloudFolder.FolderId == OtherFolderId);
            Assert.All(roots, root => Assert.Equal(CottonSyncDirection.Bidirectional, root.Direction));
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_allows_same_folder_for_different_accounts()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot = CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects");

            await _service.EnableUserSelectedDocumentTreeRootAsync(InstanceUri, "account-1", folder, localRoot);
            await _service.EnableUserSelectedDocumentTreeRootAsync(InstanceUri, "account-2", folder, localRoot);

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(InstanceUri);

            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => root.AccountScopeKey == "account-1");
            Assert.Contains(roots, root => root.AccountScopeKey == "account-2");
            Assert.Equal(2, roots.Select(root => root.StableKey).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Enable_user_selected_document_tree_root_rejects_missing_grant()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            CottonSyncLocalRootSnapshot localRoot =
                CreateDocumentTreeRoot(
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

        [Fact]
        public async Task Enable_user_selected_document_tree_root_rejects_app_private_root()
        {
            CottonUploadDestinationSnapshot folder = CreateFolder(FolderId, "Projects");
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-sync-root",
                "On this device",
                CottonSyncRootPermissionStatus.Available);

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

        private static CottonSyncLocalRootSnapshot CreateDocumentTreeRoot(
            string rootKey,
            string displayName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                rootKey,
                displayName,
                permissionStatus);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            CottonUploadDestinationSnapshot folder,
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "account-1",
                folder,
                localRoot,
                direction);
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
