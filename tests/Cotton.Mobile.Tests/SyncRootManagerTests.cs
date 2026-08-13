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
        private readonly TestUploadReceiptStore _uploadReceiptStore;
        private readonly TestPermissionResolver _permissionResolver;
        private readonly SyncRootManager _manager;

        public SyncRootManagerTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-manager-tests",
                Guid.NewGuid().ToString("N"));
            TestSyncRootMetadataPathProvider metadataPathProvider = new(_directory);
            _rootStore = new FileSystemCottonSyncRootStore(
                metadataPathProvider,
                NullLogger<FileSystemCottonSyncRootStore>.Instance, TimeProvider.System);
            _uploadReceiptStore = new TestUploadReceiptStore();
            _permissionResolver = new TestPermissionResolver();
            _manager = new SyncRootManager(
                _rootStore,
                new FileSystemCottonSyncRootPauseStore(
                    metadataPathProvider,
                    NullLogger<FileSystemCottonSyncRootPauseStore>.Instance, TimeProvider.System),
                new FileSystemCottonSyncedFileManifestStore(
                    new TestSyncedFileManifestPathProvider(_directory),
                    NullLogger<FileSystemCottonSyncedFileManifestStore>.Instance, TimeProvider.System),
                _uploadReceiptStore,
                _permissionResolver);
        }

        [Fact]
        public async Task LoadMarksRootUnavailableWhenPersistedGrantWasRevoked()
        {
            CottonSyncRootSnapshot storedRoot = CreateRoot(CottonSyncRootPermissionStatus.Available);
            await _rootStore.SaveAsync(InstanceUri, [storedRoot]);
            _permissionResolver.PermissionStatus = CottonSyncRootPermissionStatus.Revoked;

            SyncRootCollectionSnapshot collection = await _manager.LoadAsync(InstanceUri, "account-1");

            CottonSyncRootSnapshot resolvedRoot = Assert.Single(collection.Roots);
            Assert.Equal(CottonSyncRootPermissionStatus.Revoked, resolvedRoot.LocalRoot.PermissionStatus);
            Assert.Equal(CottonSyncRootReadinessStatus.GrantRevoked, resolvedRoot.ReadinessStatus);
            Assert.False(resolvedRoot.CanRunSync);
            Assert.Equal(
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload,
                resolvedRoot.UploadOriginalRetention);
            CottonSyncRootSnapshot persistedRoot = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(CottonSyncRootPermissionStatus.Available, persistedRoot.LocalRoot.PermissionStatus);
        }

        [Fact]
        public async Task LoadMarksRootReadyWhenPersistedGrantIsAvailableAgain()
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

        [Fact]
        public async Task LoadReturnsRootsOnlyForRequestedAccount()
        {
            CottonSyncRootSnapshot storedRoot = CreateRoot(CottonSyncRootPermissionStatus.Available);
            await _rootStore.SaveAsync(InstanceUri, [storedRoot]);

            SyncRootCollectionSnapshot currentAccount = await _manager.LoadAsync(InstanceUri, "account-1");
            SyncRootCollectionSnapshot otherAccount = await _manager.LoadAsync(InstanceUri, "account-2");

            Assert.Single(currentAccount.Roots);
            Assert.Empty(otherAccount.Roots);
        }

        [Fact]
        public async Task StopClearsUploadReceiptsForDeviceToCloudRoot()
        {
            CottonSyncRootSnapshot root = CreateRoot(CottonSyncRootPermissionStatus.Available);
            await _rootStore.SaveAsync(InstanceUri, [root]);
            await _uploadReceiptStore.SaveAsync(InstanceUri, root, CreatePendingReceipt());

            bool removed = await _manager.StopAsync(InstanceUri, root);

            Assert.True(removed);
            Assert.Empty(await _rootStore.LoadAsync(InstanceUri));
            Assert.Empty(await _uploadReceiptStore.LoadAsync(InstanceUri, root));
            Assert.Equal([root.Id], _uploadReceiptStore.ClearedRootIds);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncRootPermissionStatus permissionStatus)
        {
            return CreateRoot(
                permissionStatus,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncRootPermissionStatus permissionStatus,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention uploadOriginalRetention)
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
                direction,
                uploadOriginalRetention);
        }

        private static CottonUploadReceiptSnapshot CreatePendingReceipt()
        {
            CottonDeviceToCloudSyncPlanItem item = new(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                cloudItemId: null,
                expectedRemoteETag: null,
                new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc),
                sizeBytes: 42,
                contentType: "image/jpeg",
                localSourceId: "primary:DCIM/Camera/photo.jpg",
                contentHash: TestContentHashes.First);
            return CottonUploadReceiptSnapshot.CreatePending(
                item,
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                new DateTime(2026, 8, 4, 10, 1, 0, DateTimeKind.Utc));
        }
    }
}
