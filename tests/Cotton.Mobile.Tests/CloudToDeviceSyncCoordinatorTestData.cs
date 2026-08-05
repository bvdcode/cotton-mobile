using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class CloudToDeviceSyncCoordinatorTestData
    {
        public static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        public static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid SecondSyncRootId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid SecondFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly DateTime UpdatedAt = new(2026, 6, 20, 16, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime SyncedAt = new(2026, 6, 20, 16, 5, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonSyncDirection direction = CottonSyncDirection.CloudToDevice,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.AppPrivateDirectory)
        {
            string localRootName = storageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory => "On this device",
                CottonSyncRootStorageKind.UserSelectedDocumentTree => "Device folder",
                _ => throw new ArgumentOutOfRangeException(nameof(storageKind)),
            };
            return new CottonSyncRootSnapshot(
                syncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(folderId, folderName, $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    $"app-private-sync-root-{folderId:N}",
                    localRootName,
                    permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
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

        public static CottonFileBrowserEntry CreateFolder(Guid id, string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.Folder,
                name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
