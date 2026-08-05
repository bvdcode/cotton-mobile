using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncRootListPresentationTestData
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");
        public static Guid FirstRootId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid SecondRootId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid FirstFolderId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid SecondFolderId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            Guid folderId,
            string folderName,
            string path,
            CottonSyncRootPermissionStatus permissionStatus,
            CottonSyncDirection direction,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.AppPrivateDirectory,
            string displayName = "On this device")
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(folderId, folderName, path),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    "app-private-cloud-to-device",
                    displayName,
                    permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }
    }
}
