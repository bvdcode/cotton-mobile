using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootReconnectServiceTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ConflictingRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly CottonSyncRootReconnectService _service;

        public SyncRootReconnectServiceTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-reconnect-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(new FixedSyncRootMetadataPathProvider(_directory));
            _service = new CottonSyncRootReconnectService(_rootStore);
        }

        [Fact]
        public async Task Reconnect_restores_original_local_root_and_preserves_configuration()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                RootId,
                "content://tree/primary%3AOld",
                "Old",
                CottonSyncRootPermissionStatus.Revoked,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            await _rootStore.SaveAsync(InstanceUri, [root]);
            CottonSyncLocalRootSnapshot replacement = CreateLocalRoot(
                "content://tree/primary%3AOld",
                "Old",
                CottonSyncRootPermissionStatus.Available);

            CottonSyncRootSnapshot result = await _service
                .ReconnectUserSelectedDocumentTreeAsync(root, replacement);

            Assert.Equal(root.Id, result.Id);
            Assert.Equal(root.InstanceUri, result.InstanceUri);
            Assert.Equal(root.AccountScopeKey, result.AccountScopeKey);
            Assert.Same(root.CloudFolder, result.CloudFolder);
            Assert.Equal(root.Direction, result.Direction);
            Assert.Equal(root.UploadOriginalRetention, result.UploadOriginalRetention);
            Assert.Same(replacement, result.LocalRoot);
            Assert.True(result.CanRunSync);
            CottonSyncRootSnapshot saved = Assert.Single(await _rootStore.LoadAsync(InstanceUri));
            Assert.Equal(result.Id, saved.Id);
            Assert.Equal(replacement.RootKey, saved.LocalRoot.RootKey);
            Assert.Equal(root.UploadOriginalRetention, saved.UploadOriginalRetention);
            Assert.Equal(root.StableKey, saved.StableKey);
        }

        [Fact]
        public async Task Reconnect_rejects_a_different_local_root_without_replacing_existing_roots()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                RootId,
                "content://tree/primary%3AOld",
                "Old",
                CottonSyncRootPermissionStatus.Revoked,
                CottonSyncDirection.Bidirectional);
            CottonSyncRootSnapshot conflictingRoot = CreateRoot(
                ConflictingRootId,
                "content://tree/primary%3ANew",
                "New",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.Bidirectional);
            await _rootStore.SaveAsync(InstanceUri, [root, conflictingRoot]);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReconnectUserSelectedDocumentTreeAsync(
                    root,
                    CreateLocalRoot(
                        "content://tree/primary%3ANew",
                        "New",
                        CottonSyncRootPermissionStatus.Available)));

            IReadOnlyList<CottonSyncRootSnapshot> saved = await _rootStore.LoadAsync(InstanceUri);
            Assert.Equal(2, saved.Count);
            Assert.Contains(saved, candidate => candidate.Id == RootId);
            Assert.Contains(saved, candidate => candidate.Id == ConflictingRootId);
        }

        [Fact]
        public async Task Reconnect_rejects_a_root_that_does_not_need_user_action()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                RootId,
                "content://tree/primary%3AReady",
                "Ready",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.Bidirectional);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReconnectUserSelectedDocumentTreeAsync(
                    root,
                    CreateLocalRoot(
                        "content://tree/primary%3ANew",
                        "New",
                        CottonSyncRootPermissionStatus.Available)));
        }

        [Fact]
        public async Task Reconnect_rejects_a_replacement_without_an_available_grant()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                RootId,
                "content://tree/primary%3AOld",
                "Old",
                CottonSyncRootPermissionStatus.Revoked,
                CottonSyncDirection.Bidirectional);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReconnectUserSelectedDocumentTreeAsync(
                    root,
                    CreateLocalRoot(
                        "content://tree/primary%3ANew",
                        "New",
                        CottonSyncRootPermissionStatus.NeedsUserGrant)));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            string localRootKey,
            string localRootDisplayName,
            CottonSyncRootPermissionStatus permissionStatus,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention uploadOriginalRetention =
                CottonUploadOriginalRetention.KeepOriginals)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                CreateLocalRoot(localRootKey, localRootDisplayName, permissionStatus),
                direction,
                uploadOriginalRetention);
        }

        private static CottonSyncLocalRootSnapshot CreateLocalRoot(
            string rootKey,
            string displayName,
            CottonSyncRootPermissionStatus permissionStatus)
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                rootKey,
                displayName,
                permissionStatus);
        }

    }
}
