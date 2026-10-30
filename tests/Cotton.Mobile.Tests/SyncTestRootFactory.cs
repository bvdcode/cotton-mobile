using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncTestRootFactory
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");

        public static CottonSyncRootSnapshot CreateDocumentTreeRoot(
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals,
            Guid? rootId = null,
            string rootKey = "content://tree/primary%3AProjects")
        {
            return CreateRoot(
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    rootKey,
                    "Projects",
                    permissionStatus),
                retention,
                rootId);
        }

        public static CottonSyncRootSnapshot CreateMediaStoreRoot(
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            Guid? rootId = null)
        {
            return CreateRoot(
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.MediaStore,
                    "content://media/external/file",
                    "Photos and videos",
                    permissionStatus),
                CottonUploadOriginalRetention.KeepOriginals,
                rootId);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncLocalRootSnapshot localRoot,
            CottonUploadOriginalRetention retention,
            Guid? rootId)
        {
            return new CottonSyncRootSnapshot(
                rootId ?? Guid.NewGuid(),
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Projects", "Files / Projects"),
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                retention);
        }
    }
}
