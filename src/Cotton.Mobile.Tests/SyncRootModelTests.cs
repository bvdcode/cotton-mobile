using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootModelTests
    {
        [Fact]
        public void DocumentTreeRootCanDeleteConfirmedUploads()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot(
                retention: CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);

            Assert.True(root.CanRunSync);
            Assert.True(root.DeletesOriginalsAfterUpload);
            Assert.Equal(CottonSyncDirection.DeviceToCloud, root.Direction);
        }

        [Fact]
        public void MediaStoreRootMustKeepOriginals()
        {
            CottonSyncLocalRootSnapshot localRoot = new(
                CottonSyncRootStorageKind.MediaStore,
                "content://media/external/file",
                "Photos and videos",
                CottonSyncRootPermissionStatus.Available);

            Assert.Throws<ArgumentException>(() => new CottonSyncRootSnapshot(
                Guid.NewGuid(),
                SyncTestRootFactory.InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Media", "Files / Media"),
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload));
        }

        [Fact]
        public void UndefinedDirectionIsRejected()
        {
            CottonSyncRootSnapshot validRoot = SyncTestRootFactory.CreateDocumentTreeRoot();

            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonSyncRootSnapshot(
                Guid.NewGuid(),
                validRoot.InstanceUri,
                validRoot.AccountScopeKey,
                validRoot.CloudFolder,
                validRoot.LocalRoot,
                (CottonSyncDirection)42,
                CottonUploadOriginalRetention.KeepOriginals));
        }

        [Fact]
        public void RevokedMediaStorePermissionRequiresUserAction()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot(
                CottonSyncRootPermissionStatus.Revoked);

            Assert.False(root.CanRunSync);
            Assert.True(root.NeedsUserAction);
        }

        [Fact]
        public void LegacyUnscopedMediaStoreRootRequiresFolderSelection()
        {
            CottonSyncLocalRootSnapshot localRoot = new(
                CottonSyncRootStorageKind.MediaStore,
                "content://media/external/file",
                "Photos and videos",
                CottonSyncRootPermissionStatus.Available);
            CottonSyncRootSnapshot root = new(
                Guid.NewGuid(),
                SyncTestRootFactory.InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Media", "Files / Media"),
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.False(root.CanRunSync);
            Assert.True(root.NeedsUserAction);
            Assert.Equal(CottonSyncRootReadinessStatus.NeedsUserGrant, root.ReadinessStatus);
        }

        [Fact]
        public void MediaStoreScopeDoesNotChangeRootIdentity()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot();
            CottonSyncLocalRootSnapshot changedScope = new(
                root.LocalRoot.StorageKind,
                root.LocalRoot.RootKey,
                "Camera and Screenshots",
                root.LocalRoot.PermissionStatus,
                "buckets:1,2");
            CottonSyncRootSnapshot updated = new(
                root.Id,
                root.InstanceUri,
                root.AccountScopeKey,
                root.CloudFolder,
                changedScope,
                root.Direction,
                root.UploadOriginalRetention);

            Assert.Equal(root.StableKey, updated.StableKey);
        }
    }
}
