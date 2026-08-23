using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncTestRootFactory
    {
        public const string AccountScopeKey = "account-1";

        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");

        public static CottonAuthenticatedSessionScope SessionScope { get; } =
            new(InstanceUri, AccountScopeKey);

        public static CottonSyncRootSnapshot CreateDocumentTreeRoot(
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals,
            Guid? rootId = null,
            string rootKey = "content://tree/primary%3AProjects",
            string accountScopeKey = AccountScopeKey)
        {
            return CreateRoot(
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    rootKey,
                    "Projects",
                    permissionStatus),
                retention,
                rootId,
                accountScopeKey);
        }

        public static CottonSyncRootSnapshot CreateMediaStoreRoot(
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            Guid? rootId = null,
            string accountScopeKey = AccountScopeKey)
        {
            return CreateRoot(
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.MediaStore,
                    "content://media/external/file",
                    "Photos and videos",
                    permissionStatus,
                    "buckets:1"),
                CottonUploadOriginalRetention.KeepOriginals,
                rootId,
                accountScopeKey);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncLocalRootSnapshot localRoot,
            CottonUploadOriginalRetention retention,
            Guid? rootId,
            string accountScopeKey)
        {
            return new CottonSyncRootSnapshot(
                rootId ?? Guid.NewGuid(),
                InstanceUri,
                accountScopeKey,
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Projects", "Files / Projects"),
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                retention);
        }
    }
}
