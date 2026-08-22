using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class DeviceToCloudSyncCoordinatorTestData
    {
        public static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        public static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid SecondSyncRootId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid SecondFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly DateTime UpdatedAt = new(2026, 6, 20, 17, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime SyncedAt = new(2026, 6, 20, 17, 5, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonSyncDirection direction = CottonSyncDirection.DeviceToCloud,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree)
        {
            string localRootId = storageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory => $"app-private-sync-root-{folderId:N}",
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    $"content://com.android.externalstorage.documents/tree/primary%3A{folderName}",
                _ => throw new ArgumentOutOfRangeException(nameof(storageKind)),
            };
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
                new CottonSyncLocalRootSnapshot(storageKind, localRootId, localRootName, permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        public static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Device folder", items, problems: []);
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

        public static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            string? eTag,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return CottonFileBrowserEntryFactory.CreateFile(
                id,
                name,
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag: eTag,
                metadata: metadata,
                contentHash: TestContentHashes.First);
        }

        public static CottonFileBrowserEntry CreateFolder(Guid id, string name)
        {
            return CottonFileBrowserEntryFactory.CreateFolder(
                id,
                name,
                UpdatedAt);
        }
    }
}
