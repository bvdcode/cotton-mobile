using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootManagerTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly TestPermissionResolver _permissionResolver;
        private readonly SyncRootManager _manager;

        public SyncRootManagerTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-manager-tests",
                Guid.NewGuid().ToString("N"));
            TestSyncRootMetadataPathProvider metadataPathProvider = new(_directory);
            _rootStore = new FileSystemCottonSyncRootStore(metadataPathProvider);
            _permissionResolver = new TestPermissionResolver();
            _manager = new SyncRootManager(
                _rootStore,
                new FileSystemCottonSyncRootPauseStore(metadataPathProvider),
                new FileSystemCottonSyncedFileManifestStore(
                    new TestSyncedFileManifestPathProvider(_directory)),
                _permissionResolver);
        }

        [Fact]
        public async Task Load_marks_root_unavailable_when_persisted_grant_was_revoked()
        {
            CottonSyncRootSnapshot storedRoot = CreateRoot(CottonSyncRootPermissionStatus.Available);
            await _rootStore.SaveAsync(InstanceUri, [storedRoot]);
            _permissionResolver.PermissionStatus = CottonSyncRootPermissionStatus.Revoked;

            SyncRootCollectionSnapshot collection = await _manager.LoadAsync(InstanceUri, "account-1");

            CottonSyncRootSnapshot resolvedRoot = Assert.Single(collection.Roots);
            Assert.Equal(CottonSyncRootPermissionStatus.Revoked, resolvedRoot.LocalRoot.PermissionStatus);
            Assert.Equal(CottonSyncRootReadinessStatus.GrantRevoked, resolvedRoot.ReadinessStatus);
            Assert.False(resolvedRoot.CanRunSync);
            CottonSyncRootSnapshot persistedRoot = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(CottonSyncRootPermissionStatus.Available, persistedRoot.LocalRoot.PermissionStatus);
        }

        [Fact]
        public async Task Load_marks_root_ready_when_persisted_grant_is_available_again()
        {
            CottonSyncRootSnapshot storedRoot = CreateRoot(CottonSyncRootPermissionStatus.Revoked);
            await _rootStore.SaveAsync(InstanceUri, [storedRoot]);
            _permissionResolver.PermissionStatus = CottonSyncRootPermissionStatus.Available;

            SyncRootCollectionSnapshot collection = await _manager.LoadAsync(InstanceUri, "account-1");

            CottonSyncRootSnapshot resolvedRoot = Assert.Single(collection.Roots);
            Assert.Equal(CottonSyncRootPermissionStatus.Available, resolvedRoot.LocalRoot.PermissionStatus);
            Assert.Equal(CottonSyncRootReadinessStatus.Ready, resolvedRoot.ReadinessStatus);
            Assert.True(resolvedRoot.CanRunSync);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncRootPermissionStatus permissionStatus)
        {
            return new CottonSyncRootSnapshot(
                RootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://com.android.externalstorage.documents/tree/primary%3AProjects",
                    "Projects",
                    permissionStatus),
                CottonSyncDirection.Bidirectional);
        }

        private class TestPermissionResolver : ICottonSyncLocalRootPermissionResolver
        {
            public CottonSyncRootPermissionStatus PermissionStatus { get; set; } =
                CottonSyncRootPermissionStatus.Available;

            public CottonSyncRootPermissionStatus Resolve(CottonSyncLocalRootSnapshot localRoot)
            {
                ArgumentNullException.ThrowIfNull(localRoot);
                return PermissionStatus;
            }
        }

        private class TestSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
        {
            private readonly string _directory;

            public TestSyncRootMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncRootMetadataDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }

        private class TestSyncedFileManifestPathProvider : ICottonSyncedFileManifestPathProvider
        {
            private readonly string _directory;

            public TestSyncedFileManifestPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncedFileManifestDirectory(
                Uri instanceUri,
                CottonSyncRootSnapshot root)
            {
                return Path.Combine(_directory, "manifests", root.Id.ToString("N"));
            }
        }
    }
}
