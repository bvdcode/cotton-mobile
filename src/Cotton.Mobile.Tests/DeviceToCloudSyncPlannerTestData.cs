using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    internal static class DeviceToCloudSyncPlannerTestData
    {
        public static Guid SyncRootId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid FolderId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid FirstFileId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid SecondFileId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid OperationId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static DateTime SyncedAt { get; } = new(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        public static void AssertUploadedReceiptIsKept(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFile, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.True(item.IsNoOp);
            Assert.False(plan.HasExecutableChanges);
        }

        public static void AssertPendingLocalChangeIsBlocked(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
            Assert.True(plan.HasBlockingItems);
        }

        public static void AssertUploadedReceiptDeleteIsBlocked(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.False(item.RequiresLocalDelete);
            Assert.True(item.IsBlocked);
        }

        public static CottonSyncRootSnapshot CreateReadyRoot(
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals)
        {
            return CreateRoot(CottonSyncDirection.DeviceToCloud, retention);
        }

        public static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction,
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                direction,
                retention);
        }

        public static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        public static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            DateTime updatedAt,
            long sizeBytes,
            string? localSourceId,
            string contentHash = TestContentHashes.First)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                updatedAt,
                sizeBytes,
                "text/plain",
                localSourceId,
                contentHash);
        }

        public static CottonDeviceToCloudLocalItemSnapshot CreateLocalFolder(string name, string relativePath)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFolder(name, relativePath, SyncedAt);
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
            string eTag,
            Guid? operationId = null,
            long sizeBytes = 42,
            string contentHash = TestContentHashes.First)
        {
            IReadOnlyDictionary<string, string>? metadata = operationId.HasValue
                ? new Dictionary<string, string>
                {
                    [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.Value.ToString("N"),
                }
                : null;
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.CreateFile(
                id,
                name,
                SyncedAt,
                sizeBytes,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag,
                metadata,
                contentHash);
            return new CottonDeviceToCloudRemoteItemSnapshot(entry, relativePath);
        }

        public static CottonUploadReceiptSnapshot CreatePendingReceipt(string relativePath = "alpha.txt")
        {
            return new CottonUploadReceiptSnapshot(
                "document-alpha",
                relativePath,
                SyncedAt,
                42,
                "text/plain",
                OperationId,
                CottonUploadReceiptStatus.Pending,
                SyncedAt.AddMinutes(1),
                remoteFileId: null,
                remoteETag: null,
                TestContentHashes.First);
        }

        public static CottonUploadReceiptSnapshot CreateUploadedReceipt(
            string? contentHash = TestContentHashes.First)
        {
            return new CottonUploadReceiptSnapshot(
                "document-alpha",
                "alpha.txt",
                SyncedAt,
                42,
                "text/plain",
                OperationId,
                CottonUploadReceiptStatus.Uploaded,
                SyncedAt.AddMinutes(1),
                FirstFileId,
                "\"etag-1\"",
                contentHash);
        }
    }
}
