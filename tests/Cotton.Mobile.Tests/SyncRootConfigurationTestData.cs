using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncRootConfigurationTestData
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");
        public static Guid FolderId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        public static Task<CottonSyncRootConfigurationResult> ConfigureDefaultRootAsync(
            this CottonSyncRootConfigurationService service,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention retention)
        {
            return service.ConfigureUserSelectedDocumentTreeRootAsync(
                InstanceUri,
                "account-1",
                CreateFolder(FolderId, "Projects"),
                CreateDocumentTreeRoot("content://tree/primary%3AProjects", "Projects"),
                direction,
                retention);
        }

        public static CottonUploadDestinationSnapshot CreateFolder(Guid folderId, string folderName)
        {
            return new CottonUploadDestinationSnapshot(folderId, folderName, $"Files / {folderName}");
        }

        public static CottonSyncLocalRootSnapshot CreateDocumentTreeRoot(
            string rootKey,
            string displayName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                rootKey,
                displayName,
                permissionStatus);
        }

        public static CottonSyncRootSnapshot CreateRoot(
            Uri instanceUri,
            Guid rootId,
            CottonUploadDestinationSnapshot folder,
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention retention)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                instanceUri,
                "account-1",
                folder,
                localRoot,
                direction,
                retention);
        }
    }
}
