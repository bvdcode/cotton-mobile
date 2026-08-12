using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class CloudToDeviceSyncPlanExecutorTestData
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");
        public static Guid SyncRootId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid FolderId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid FirstFileId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid SecondFileId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid ThirdFileId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static DateTime UpdatedAt { get; } =
            new(2026, 6, 20, 15, 0, 0, DateTimeKind.Utc);
        public static DateTime SyncedAt { get; } =
            new(2026, 6, 20, 15, 5, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(Guid folderId)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(folderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        public static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(FolderId, "Projects", entries);
        }

        public static CottonFileBrowserEntry CreateFile(Guid id, string name, string? eTag)
        {
            return CottonFileBrowserEntryFactory.CreateFile(
                id,
                name,
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag,
                contentHash: TestContentHashes.First);
        }

        public static CottonFileBrowserEntry CreateFolder(string name)
        {
            return CottonFileBrowserEntryFactory.CreateFolder(
                Guid.NewGuid(),
                name,
                UpdatedAt);
        }
    }
}
