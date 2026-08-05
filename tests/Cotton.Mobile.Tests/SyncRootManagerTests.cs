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
            _rootStore = new FileSystemCottonSyncRootStore(metadataPathProvider);
            _uploadReceiptStore = new TestUploadReceiptStore();
            _permissionResolver = new TestPermissionResolver();
            _manager = new SyncRootManager(
                _rootStore,
                new FileSystemCottonSyncRootPauseStore(metadataPathProvider),
                new FileSystemCottonSyncedFileManifestStore(
                    new TestSyncedFileManifestPathProvider(_directory)),
                _uploadReceiptStore,
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
            Assert.Equal(
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload,
                resolvedRoot.UploadOriginalRetention);
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

        [Fact]
        public async Task Stop_clears_upload_receipts_for_device_to_cloud_root()
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

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice)]
        [InlineData(CottonSyncDirection.Bidirectional)]
        public async Task Stop_does_not_clear_upload_receipts_for_other_directions(
            CottonSyncDirection direction)
        {
            CottonSyncRootSnapshot root = CreateRoot(
                CottonSyncRootPermissionStatus.Available,
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
            await _rootStore.SaveAsync(InstanceUri, [root]);

            bool removed = await _manager.StopAsync(InstanceUri, root);

            Assert.True(removed);
            Assert.Empty(_uploadReceiptStore.ClearedRootIds);
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

        private class TestUploadReceiptStore : ICottonUploadReceiptStore
        {
            private readonly Dictionary<Guid, List<CottonUploadReceiptSnapshot>> _receiptsByRootId = [];

            public List<Guid> ClearedRootIds { get; } = [];

            public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<CottonUploadReceiptSnapshot> receipts = _receiptsByRootId.TryGetValue(
                    root.Id,
                    out List<CottonUploadReceiptSnapshot>? storedReceipts)
                    ? storedReceipts.ToArray()
                    : [];
                return Task.FromResult(receipts);
            }

            public Task SaveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonUploadReceiptSnapshot receipt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_receiptsByRootId.TryGetValue(
                    root.Id,
                    out List<CottonUploadReceiptSnapshot>? receipts))
                {
                    receipts = [];
                    _receiptsByRootId[root.Id] = receipts;
                }

                receipts.RemoveAll(item => item.LocalSourceId == receipt.LocalSourceId);
                receipts.Add(receipt);
                return Task.CompletedTask;
            }

            public Task ClearAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (root.Direction != CottonSyncDirection.DeviceToCloud)
                {
                    throw new InvalidOperationException("Upload receipts are unsupported for this sync direction.");
                }

                ClearedRootIds.Add(root.Id);
                _receiptsByRootId.Remove(root.Id);
                return Task.CompletedTask;
            }
        }
    }
}
