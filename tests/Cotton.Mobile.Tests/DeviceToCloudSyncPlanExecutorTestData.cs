using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class DeviceToCloudSyncPlanExecutorTestData
    {
        public static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        public static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid RootFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid ExistingFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid CreatedFolderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ThirdFileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly DateTime UpdatedAt = new(2026, 6, 20, 15, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime SyncedAt = new(2026, 6, 20, 15, 5, 0, DateTimeKind.Utc);

        public static CottonSyncRootSnapshot CreateRoot(Guid folderId)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(folderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://com.android.externalstorage.documents/tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.Bidirectional,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        public static CottonDeviceToCloudSyncPlanSnapshot CreatePlan(
            params CottonDeviceToCloudSyncPlanItem[] items)
        {
            return new CottonDeviceToCloudSyncPlanSnapshot(SyncRootId, RootFolderId, "Projects", items);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateUploadNewFile(
            string name,
            string relativePath)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                name,
                relativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: UpdatedAt,
                localSourceId: $"source:{relativePath}");
        }

        public static CottonDeviceToCloudSyncPlanItem CreateUploadChangedFile(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadChangedFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                UpdatedAt,
                $"source:{relativePath}");
        }

        public static CottonDeviceToCloudSyncPlanItem CreateExistingFile(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.KeepExistingFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                UpdatedAt,
                $"source:{relativePath}");
        }

        public static CottonDeviceToCloudSyncPlanItem CreateRemoteFileDelete(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.DeleteRemoteFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                localSourceId: null);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateManifestOrphanRemoval(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                localSourceId: null);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateBlockedItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath)
        {
            return CreateFilePlanItem(
                action,
                name,
                relativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: UpdatedAt,
                localSourceId: $"source:{relativePath}");
        }

        public static CottonDeviceToCloudSyncPlanItem CreateRemoteFolder(
            string name,
            string relativePath)
        {
            return CreateFolderPlanItem(
                CottonDeviceToCloudSyncActionKind.CreateRemoteFolder,
                name,
                relativePath,
                cloudItemId: null,
                localSourceId: $"source:{relativePath}");
        }

        public static CottonDeviceToCloudSyncPlanItem CreateExistingRemoteFolder(
            Guid cloudItemId,
            string name,
            string relativePath)
        {
            return CreateFolderPlanItem(
                CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                name,
                relativePath,
                cloudItemId,
                localSourceId: $"source:{relativePath}");
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

        public static CottonFileBrowserEntry CreateFolder(Guid id, string name)
        {
            return CottonFileBrowserEntryFactory.CreateFolder(
                id,
                name,
                UpdatedAt);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateFilePlanItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            DateTime? localUpdatedAtUtc,
            string? localSourceId)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc,
                42,
                "text/plain",
                localSourceId,
                uploadOperationId: null,
                TestContentHashes.First);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateFolderPlanItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath,
            Guid? cloudItemId,
            string? localSourceId)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.Folder,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag: null,
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                localSourceId);
        }
    }
}
