using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncSettingsRunStatusTestData
    {
        public static Uri InstanceUri { get; } = new("https://app.cottoncloud.dev");
        public static Guid CloudRootId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid DeviceRootId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid BidirectionalRootId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static Guid SkippedDeviceRootId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static Guid FolderId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static CottonCloudToDeviceSyncRunSummary CreateCloudSummary(
            CottonCloudToDeviceSyncExecutionResult executionResult)
        {
            CottonSyncRootSnapshot root = CreateCloudRoot();
            CottonCloudToDeviceSyncPlanSnapshot plan = new(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);
            return new CottonCloudToDeviceSyncRunSummary(
                [CottonCloudToDeviceSyncRootRunResult.Completed(root, plan, executionResult)]);
        }

        public static CottonDeviceToCloudSyncRunSummary CreateDeviceSummary(
            CottonDeviceToCloudSyncExecutionResult executionResult,
            params CottonDeviceToCloudSyncRootRunResult[] extraResults)
        {
            CottonSyncRootSnapshot root = CreateDeviceRoot(DeviceRootId);
            CottonDeviceToCloudSyncPlanSnapshot plan = new(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);
            List<CottonDeviceToCloudSyncRootRunResult> results =
            [
                CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult), .. extraResults,
            ];
            return new CottonDeviceToCloudSyncRunSummary(results);
        }

        public static CottonBidirectionalSyncRunSummary CreateBidirectionalSummary(
            CottonCloudToDeviceSyncExecutionResult cloudExecutionResult,
            CottonDeviceToCloudSyncExecutionResult deviceExecutionResult)
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            CottonBidirectionalSyncExecutionPlan executionPlan = CreateBidirectionalExecutionPlan(root, []);
            return new CottonBidirectionalSyncRunSummary(
                [
                    CottonBidirectionalSyncRootRunResult.Completed(
                        root,
                        executionPlan,
                        cloudExecutionResult,
                        deviceExecutionResult),
                ]);
        }

        public static CottonBidirectionalSyncRunSummary CreateBidirectionalDestructiveReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            CottonBidirectionalSyncExecutionPlan plan = CreateBidirectionalExecutionPlan(
                root,
                [
                    new CottonBidirectionalSyncPlanItem(
                        CottonBidirectionalSyncActionKind.DeleteRemoteFile,
                        CottonFileBrowserEntryType.File,
                        "old.txt",
                        "old.txt",
                        previousRelativePath: null,
                        Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        "\"etag-old\"",
                        localUpdatedAtUtc: null,
                        remoteUpdatedAtUtc: DateTime.UtcNow,
                        sizeBytes: 12,
                        contentType: "text/plain"),
                ]);
            return new CottonBidirectionalSyncRunSummary(
                [CottonBidirectionalSyncRootRunResult.SkippedDestructiveReviewRequired(root, plan)]);
        }

        public static CottonBidirectionalSyncRunSummary CreateBidirectionalConflictReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            CottonBidirectionalSyncExecutionPlan plan = CreateBidirectionalExecutionPlan(
                root,
                [
                    new CottonBidirectionalSyncPlanItem(
                        CottonBidirectionalSyncActionKind.FileChangedOnBothSides,
                        CottonFileBrowserEntryType.File,
                        "conflict.txt",
                        "conflict.txt",
                        previousRelativePath: null,
                        Guid.Parse("88888888-8888-8888-8888-888888888888"),
                        "\"etag-remote\"",
                        localUpdatedAtUtc: DateTime.UtcNow.AddMinutes(-1),
                        remoteUpdatedAtUtc: DateTime.UtcNow,
                        sizeBytes: 12,
                        contentType: "text/plain"),
                ]);
            return new CottonBidirectionalSyncRunSummary(
                [CottonBidirectionalSyncRootRunResult.SkippedConflictReviewRequired(root, plan)]);
        }

        public static CottonBidirectionalSyncRunSummary CreateBidirectionalBlockedReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            CottonBidirectionalSyncExecutionPlan plan = CreateBidirectionalExecutionPlan(
                root,
                [
                    new CottonBidirectionalSyncPlanItem(
                        CottonBidirectionalSyncActionKind.NeedsFreshServerRevision,
                        CottonFileBrowserEntryType.File,
                        "needs-fresh.txt",
                        "needs-fresh.txt",
                        previousRelativePath: null,
                        Guid.Parse("77777777-7777-7777-7777-777777777777"),
                        expectedRemoteETag: null,
                        localUpdatedAtUtc: null,
                        remoteUpdatedAtUtc: DateTime.UtcNow,
                        sizeBytes: 12,
                        contentType: "text/plain"),
                ]);
            return new CottonBidirectionalSyncRunSummary(
                [CottonBidirectionalSyncRootRunResult.SkippedBlockedReviewRequired(root, plan)]);
        }

        public static CottonSyncRootSnapshot CreateCloudRoot()
        {
            return CreateRoot(
                CloudRootId,
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-cloud-to-device",
                "Projects",
                "On this device",
                CottonSyncDirection.CloudToDevice);
        }

        public static CottonSyncRootSnapshot CreateDeviceRoot(Guid rootId)
        {
            return CreateRoot(
                rootId,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/camera",
                "Camera",
                "Camera",
                CottonSyncDirection.DeviceToCloud);
        }

        public static CottonSyncRootSnapshot CreateBidirectionalRoot()
        {
            return CreateRoot(
                BidirectionalRootId,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/projects",
                "Projects",
                "Projects",
                CottonSyncDirection.Bidirectional);
        }

        private static CottonBidirectionalSyncExecutionPlan CreateBidirectionalExecutionPlan(
            CottonSyncRootSnapshot root,
            IReadOnlyList<CottonBidirectionalSyncPlanItem> items)
        {
            CottonBidirectionalSyncPlanSnapshot preflightPlan = new(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                items);
            return new CottonBidirectionalSyncExecutionPlan(
                preflightPlan,
                new CottonCloudToDeviceSyncPlanSnapshot(
                    root.Id,
                    root.CloudFolder.FolderId,
                    root.CloudFolder.FolderName,
                    []),
                new CottonDeviceToCloudSyncPlanSnapshot(
                    root.Id,
                    root.CloudFolder.FolderId,
                    root.CloudFolder.FolderName,
                    []));
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            CottonSyncRootStorageKind storageKind,
            string rootKey,
            string folderName,
            string localRootName,
            CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(FolderId, folderName, $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    rootKey,
                    localRootName,
                    CottonSyncRootPermissionStatus.Available),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }
    }
}
