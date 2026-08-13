using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootRunCapabilityTests
    {
        [Fact]
        public void ReadyDocumentTreeAndMediaStoreRootsCanRun()
        {
            Assert.True(CottonSyncRootRunCapability.CanRun(
                SyncTestRootFactory.CreateDocumentTreeRoot()));
            Assert.True(CottonSyncRootRunCapability.CanRun(
                SyncTestRootFactory.CreateMediaStoreRoot()));
        }

        [Fact]
        public void RevokedRootCannotRun()
        {
            Assert.False(CottonSyncRootRunCapability.CanRun(
                SyncTestRootFactory.CreateMediaStoreRoot(CottonSyncRootPermissionStatus.Revoked)));
        }

        [Fact]
        public void AppPrivateStorageIsNotAnUploadSource()
        {
            CottonSyncRootSnapshot root = new(
                Guid.NewGuid(),
                SyncTestRootFactory.InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Files", "Files"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);

            Assert.False(CottonSyncRootRunCapability.CanRun(root));
            Assert.True(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(root));
        }
    }
}
