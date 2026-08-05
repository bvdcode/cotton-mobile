using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class BidirectionalSyncCoordinatorTestData
    {
        public static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        public static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid RemoteFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid UploadedFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid OldFileId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        public static readonly DateTime UpdatedAt = new(2026, 6, 20, 20, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime SyncedAt = new(2026, 6, 20, 20, 5, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(
            Guid? rootId = null,
            CottonSyncDirection direction = CottonSyncDirection.Bidirectional,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree,
            string rootKey = "content://tree/projects",
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncRootSnapshot(
                rootId ?? SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(storageKind, rootKey, "Projects", permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        public static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        public static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            string localSourceId)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                UpdatedAt,
                42,
                "text/plain",
                localSourceId,
                TestContentHashes.First);
        }

        public static CottonFolderContent CreateContent(
            CottonSyncRootSnapshot root,
            params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(root.CloudFolder.FolderId, root.CloudFolder.FolderName, entries);
        }

        public static CottonFileBrowserEntry CreateFile(Guid id, string name, string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.File,
                name,
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag,
                TestContentHashes.First);
        }
    }
}
