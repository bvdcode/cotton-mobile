using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class BidirectionalSyncPlannerTestData
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");
        public static Guid SyncRootId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid FolderId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid FileId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static Guid NewRemoteFileId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static DateTime RemoteUpdatedAt { get; } =
            new(2026, 6, 20, 18, 0, 0, DateTimeKind.Utc);
        public static DateTime SyncedAt { get; } =
            new(2026, 6, 20, 18, 5, 0, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction = CottonSyncDirection.Bidirectional,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/projects",
                    "Projects",
                    permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        public static CottonSyncedFileSnapshot CreateManifest(string eTag, long sizeBytes)
        {
            return new CottonSyncedFileSnapshot(
                FileId,
                "notes.txt",
                eTag,
                RemoteUpdatedAt,
                sizeBytes,
                "text/plain",
                SyncedAt,
                "notes.txt",
                TestContentHashes.First);
        }

        public static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        public static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            long sizeBytes,
            DateTime updatedAtUtc,
            string contentHash = TestContentHashes.First)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                updatedAtUtc,
                sizeBytes,
                "text/plain",
                $"local:{relativePath}",
                contentHash);
        }

        public static CottonDeviceToCloudRemoteContentSnapshot CreateRemoteContent(
            params CottonDeviceToCloudRemoteItemSnapshot[] items)
        {
            return new CottonDeviceToCloudRemoteContentSnapshot(FolderId, "Projects", items);
        }

        public static CottonDeviceToCloudRemoteItemSnapshot CreateRemoteFile(
            Guid id,
            string name,
            string relativePath,
            string? eTag,
            long sizeBytes,
            string contentHash = TestContentHashes.First)
        {
            return new CottonDeviceToCloudRemoteItemSnapshot(
                CottonFileBrowserEntry.CreateCached(
                    id,
                    CottonFileBrowserEntryType.File,
                    name,
                    "Text",
                    $"{sizeBytes} B · Text",
                    "More",
                    "TXT",
                    RemoteUpdatedAt,
                    sizeBytes,
                    "text/plain",
                    previewHashEncryptedHex: null,
                    eTag,
                    contentHash),
                relativePath);
        }
    }
}
