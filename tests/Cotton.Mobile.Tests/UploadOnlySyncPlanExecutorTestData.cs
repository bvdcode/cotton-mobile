using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class UploadOnlySyncPlanExecutorTestData
    {
        public static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        public static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid RootFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid RemoteFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid OperationId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly DateTime LocalUpdatedAt =
            new(2026, 8, 4, 12, 30, 0, DateTimeKind.Utc);
        public static readonly DateTime RecordedAt =
            new(2026, 8, 4, 12, 31, 0, DateTimeKind.Utc);

        public static CottonDeviceToCloudSyncPlanSnapshot CreatePlan(
            params CottonDeviceToCloudSyncPlanItem[] items)
        {
            return new CottonDeviceToCloudSyncPlanSnapshot(SyncRootId, RootFolderId, "Camera", items);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateUploadItem(
            Guid? operationId = null,
            string contentHash = TestContentHashes.First)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                cloudItemId: null,
                expectedRemoteETag: null,
                LocalUpdatedAt,
                sizeBytes: 42,
                contentType: "image/jpeg",
                localSourceId: "primary:DCIM/Camera/photo.jpg",
                uploadOperationId: operationId,
                contentHash);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateConfirmationItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                RemoteFileId,
                "etag-remote",
                LocalUpdatedAt,
                42,
                "image/jpeg",
                "primary:DCIM/Camera/photo.jpg",
                OperationId,
                TestContentHashes.First);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateCleanupItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                RemoteFileId,
                "etag-remote",
                LocalUpdatedAt,
                42,
                "image/jpeg",
                "primary:DCIM/Camera/photo.jpg",
                OperationId,
                TestContentHashes.First);
        }

        public static CottonSyncRootSnapshot CreateRoot(CottonUploadOriginalRetention retention)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(RootFolderId, "Camera", "Files / Camera"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/camera",
                    "Camera",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud,
                retention);
        }
    }
}
