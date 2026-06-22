using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootRunCapabilityTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Run_capability_matches_direction_and_local_root_kind()
        {
            CottonSyncRootSnapshot cloudAppPrivate = CreateRoot(
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.AppPrivateDirectory);
            CottonSyncRootSnapshot cloudDocumentTree = CreateRoot(
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            CottonSyncRootSnapshot cloudNeedsGrant = CreateRoot(
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                CottonSyncRootPermissionStatus.NeedsUserGrant);
            CottonSyncRootSnapshot deviceDocumentTree = CreateRoot(
                CottonSyncDirection.DeviceToCloud,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            CottonSyncRootSnapshot deviceAppPrivate = CreateRoot(
                CottonSyncDirection.DeviceToCloud,
                CottonSyncRootStorageKind.AppPrivateDirectory);
            CottonSyncRootSnapshot bidirectionalDocumentTree = CreateRoot(
                CottonSyncDirection.Bidirectional,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            CottonSyncRootSnapshot bidirectionalAppPrivate = CreateRoot(
                CottonSyncDirection.Bidirectional,
                CottonSyncRootStorageKind.AppPrivateDirectory);

            Assert.True(CottonSyncRootRunCapability.CanRun(cloudAppPrivate));
            Assert.True(CottonSyncRootRunCapability.CanRun(cloudDocumentTree));
            Assert.False(CottonSyncRootRunCapability.CanRun(cloudNeedsGrant));
            Assert.True(CottonSyncRootRunCapability.CanRun(deviceDocumentTree));
            Assert.False(CottonSyncRootRunCapability.CanRun(deviceAppPrivate));
            Assert.True(CottonSyncRootRunCapability.CanRun(bidirectionalDocumentTree));
            Assert.False(CottonSyncRootRunCapability.CanRun(bidirectionalAppPrivate));

            Assert.False(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(cloudAppPrivate));
            Assert.False(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(cloudDocumentTree));
            Assert.False(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(cloudNeedsGrant));
            Assert.False(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(deviceDocumentTree));
            Assert.True(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(deviceAppPrivate));
            Assert.False(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(bidirectionalDocumentTree));
            Assert.True(CottonSyncRootRunCapability.HasUnsupportedLocalRoot(bidirectionalAppPrivate));

            Assert.Equal(
                "Local sync target unsupported",
                CottonSyncRootRunCapability.CreateUnsupportedLocalRootStatusText(cloudAppPrivate));
            Assert.Equal(
                "Local sync source unsupported",
                CottonSyncRootRunCapability.CreateUnsupportedLocalRootStatusText(deviceAppPrivate));
            Assert.Equal(
                "Local sync source unsupported",
                CottonSyncRootRunCapability.CreateUnsupportedLocalRootStatusText(bidirectionalAppPrivate));
        }

        [Fact]
        public void Null_root_is_rejected()
        {
            Assert.Throws<ArgumentNullException>(() => CottonSyncRootRunCapability.CanRun(null!));
            Assert.Throws<ArgumentNullException>(() => CottonSyncRootRunCapability.HasUnsupportedLocalRoot(null!));
            Assert.Throws<ArgumentNullException>(
                () => CottonSyncRootRunCapability.CreateUnsupportedLocalRootStatusText(null!));
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction,
            CottonSyncRootStorageKind storageKind,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncRootSnapshot(
                RootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    storageKind == CottonSyncRootStorageKind.AppPrivateDirectory
                        ? $"app-private-sync-root-{FolderId:N}"
                        : "content://tree/projects",
                    storageKind == CottonSyncRootStorageKind.AppPrivateDirectory
                        ? "On this device"
                        : "Projects",
                    permissionStatus),
                direction);
        }
    }
}
