using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncSettingsRunStatusTextTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid CloudRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid DeviceRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid BidirectionalRootId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid SkippedDeviceRootId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Combined_status_reports_no_roots()
        {
            Assert.Equal("Syncing folders...", CottonSyncSettingsRunStatusText.StartingAllStatus);
            Assert.Equal("Offline. Sync needs internet.", CottonSyncSettingsRunStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync failed.", CottonSyncSettingsRunStatusText.FailedStatus);
            Assert.Equal(
                "No folders are set to sync.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonCloudToDeviceSyncRunSummary([]),
                    new CottonDeviceToCloudSyncRunSummary([])));
        }

        [Fact]
        public void Combined_status_reports_cloud_and_device_results()
        {
            CottonCloudToDeviceSyncRunSummary cloudSummary = CreateCloudSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 1,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));
            CottonDeviceToCloudSyncRunSummary deviceSummary = CreateDeviceSummary(
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 2,
                    refreshedCount: 1,
                    createdFolderCount: 1,
                    deletedRemoteFileCount: 1,
                    removedManifestCount: 1,
                    skippedCount: 0,
                    blockedCount: 1),
                CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedLocalRoot(
                    CreateDeviceRoot(SkippedDeviceRootId)));

            Assert.Equal(
                "Sync complete. 1 downloaded, 2 uploaded, 1 updated, 1 folder created, "
                + "1 remote file removed, 1 record cleaned, 1 blocked, 1 root skipped.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(cloudSummary, deviceSummary));
        }

        [Fact]
        public void Combined_status_reports_bidirectional_results()
        {
            CottonBidirectionalSyncRunSummary bidirectionalSummary = CreateBidirectionalSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 1,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 0),
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 2,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));

            Assert.Equal(
                "Sync complete. 1 bidirectional downloaded, 2 bidirectional uploaded.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonCloudToDeviceSyncRunSummary([]),
                    new CottonDeviceToCloudSyncRunSummary([]),
                    bidirectionalSummary));
        }

        [Fact]
        public void Combined_status_reports_device_to_cloud_destructive_review()
        {
            CottonDeviceToCloudSyncRunSummary deviceSummary = CreateDeviceDestructiveReviewSummary();

            Assert.Equal(
                "Sync complete. 1 cloud removal needs review, 1 root skipped.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonCloudToDeviceSyncRunSummary([]),
                    deviceSummary));
        }

        [Fact]
        public void Single_root_status_reports_destructive_review_cancellation()
        {
            Assert.Equal(
                "Sync cancelled.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(
                    CreateDeviceDestructiveReviewSummary()));
            Assert.Equal(
                "Sync cancelled.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(
                    CreateBidirectionalDestructiveReviewSummary()));
        }

        [Fact]
        public void Single_root_status_reports_bidirectional_conflict_review()
        {
            Assert.Equal(
                "Bidirectional sync needs conflict review.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(
                    CreateBidirectionalConflictReviewSummary()));
        }

        [Fact]
        public void Single_root_status_reports_bidirectional_blocked_review()
        {
            Assert.Equal(
                "Bidirectional sync needs review before it can run.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(
                    CreateBidirectionalBlockedReviewSummary()));
        }

        [Fact]
        public void Combined_status_reports_bidirectional_blocked_review()
        {
            Assert.Equal(
                "Sync complete. 1 blocked, 1 root skipped.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonCloudToDeviceSyncRunSummary([]),
                    new CottonDeviceToCloudSyncRunSummary([]),
                    CreateBidirectionalBlockedReviewSummary()));
        }

        [Fact]
        public void Single_root_status_reports_completed_results()
        {
            CottonCloudToDeviceSyncRunSummary cloudSummary = CreateCloudSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 1,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));
            CottonCloudToDeviceSyncRunSummary blockedCloudSummary = CreateCloudSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 0,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 1));
            CottonDeviceToCloudSyncRunSummary deviceSummary = CreateDeviceSummary(
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 1,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));
            CottonBidirectionalSyncRunSummary bidirectionalSummary = CreateBidirectionalSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 0,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 0),
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 1,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));

            Assert.Equal(
                "Sync complete. 1 downloaded.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(cloudSummary));
            Assert.Equal(
                "Sync complete. 1 blocked.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(blockedCloudSummary));
            Assert.Equal(
                "Sync complete. 1 uploaded.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(deviceSummary));
            Assert.Equal(
                "Bidirectional sync complete. 1 uploaded.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(bidirectionalSummary));
        }

        [Fact]
        public void Bidirectional_status_copy_is_stable()
        {
            CottonBidirectionalSyncRunSummary summary = CreateBidirectionalSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 0,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 0,
                    blockedCount: 0),
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));

            Assert.Equal("Sync both ways", CottonBidirectionalSyncStatusText.ActionLabel);
            Assert.Equal("Sync needs a fresh account session.", CottonBidirectionalSyncStatusText.AccountUnavailableStatus);
            Assert.Equal("Offline. Sync needs internet.", CottonBidirectionalSyncStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync cancelled.", CottonBidirectionalSyncStatusText.CancelledStatus);
            Assert.Equal("Sync failed.", CottonBidirectionalSyncStatusText.FailedStatus);
            Assert.Equal("Syncing Projects both ways...", CottonBidirectionalSyncStatusText.CreateStartingStatus(" Projects "));
            Assert.Equal(
                "Bidirectional sync needs review before it can run.",
                CottonBidirectionalSyncStatusText.BlockedReviewRequiredStatus);
            Assert.Equal("Run bidirectional sync?", CottonBidirectionalSyncStatusText.ConfirmDestructiveTitle);
            Assert.Equal("Sync", CottonBidirectionalSyncStatusText.ConfirmDestructiveAction);
            Assert.Equal(
                "This sync will remove 1 local file based on the selected folder and cloud state.",
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(1, 0));
            Assert.Equal(
                "This sync will move 2 cloud files to trash based on the selected folder and cloud state.",
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(0, 2));
            Assert.Equal(
                "This sync will remove 1 local file and move 2 cloud files to trash "
                + "based on the selected folder and cloud state.",
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(1, 2));
            Assert.Equal(
                "Bidirectional sync complete. Everything is up to date.",
                CottonBidirectionalSyncStatusText.CreateCompletedStatus(summary));
        }

        [Fact]
        public void Device_to_cloud_status_copy_is_stable()
        {
            CottonDeviceToCloudSyncRunSummary summary = CreateDeviceSummary(
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 2,
                    blockedCount: 0));
            CottonDeviceToCloudSyncRunSummary blockedSummary = CreateDeviceSummary(
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 1));

            Assert.Equal("Sync from folder", CottonDeviceToCloudSyncStatusText.ActionLabel);
            Assert.Equal("Syncing Camera...", CottonDeviceToCloudSyncStatusText.CreateStartingStatus(" Camera "));
            Assert.Equal("Sync needs a fresh account session.", CottonDeviceToCloudSyncStatusText.AccountUnavailableStatus);
            Assert.Equal("Offline. Sync needs internet.", CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync cancelled.", CottonDeviceToCloudSyncStatusText.CancelledStatus);
            Assert.Equal("Sync failed.", CottonDeviceToCloudSyncStatusText.FailedStatus);
            Assert.Equal(
                "This local folder already syncs from cloud. Stop that sync first.",
                CottonDeviceToCloudSyncStatusText.DirectionConflictStatus);
            Assert.Equal(
                "Sync needs review before removing cloud files.",
                CottonDeviceToCloudSyncStatusText.DestructiveReviewRequiredStatus);
            Assert.Equal("Sync from device folder?", CottonDeviceToCloudSyncStatusText.ConfirmDestructiveTitle);
            Assert.Equal(
                "Files removed from the selected device folder may be moved to trash in Cotton Cloud for this sync root.",
                CottonDeviceToCloudSyncStatusText.ConfirmDestructiveMessage);
            Assert.Equal("Sync", CottonDeviceToCloudSyncStatusText.ConfirmDestructiveAction);
            Assert.Equal("Move cloud files to trash?", CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteTitle);
            Assert.Equal("Move to trash", CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteAction);
            Assert.Equal(
                "This sync will move 1 cloud file to trash because it is missing from the selected device folder.",
                CottonDeviceToCloudSyncStatusText.CreateConfirmRemoteDeleteMessage(1));
            Assert.Equal(
                "This sync will move 2 cloud files to trash because they are missing from the selected device folder.",
                CottonDeviceToCloudSyncStatusText.CreateConfirmRemoteDeleteMessage(2));
            Assert.Equal(
                "Sync complete. Everything is up to date.",
                CottonDeviceToCloudSyncStatusText.CreateCompletedStatus(summary));
            Assert.Equal(
                "Sync complete. 1 blocked.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(blockedSummary));
        }

        private static CottonCloudToDeviceSyncRunSummary CreateCloudSummary(
            CottonCloudToDeviceSyncExecutionResult executionResult)
        {
            CottonSyncRootSnapshot root = CreateCloudRoot();
            var plan = new CottonCloudToDeviceSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);

            return new CottonCloudToDeviceSyncRunSummary(
                [
                    CottonCloudToDeviceSyncRootRunResult.Completed(root, plan, executionResult),
                ]);
        }

        private static CottonDeviceToCloudSyncRunSummary CreateDeviceSummary(
            CottonDeviceToCloudSyncExecutionResult executionResult,
            params CottonDeviceToCloudSyncRootRunResult[] extraResults)
        {
            CottonSyncRootSnapshot root = CreateDeviceRoot(DeviceRootId);
            var plan = new CottonDeviceToCloudSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);
            var results = new List<CottonDeviceToCloudSyncRootRunResult>
            {
                CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult),
            };
            results.AddRange(extraResults);

            return new CottonDeviceToCloudSyncRunSummary(results);
        }

        private static CottonDeviceToCloudSyncRunSummary CreateDeviceDestructiveReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateDeviceRoot(DeviceRootId);
            var plan = new CottonDeviceToCloudSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                [
                    new CottonDeviceToCloudSyncPlanItem(
                        CottonDeviceToCloudSyncActionKind.DeleteRemoteFile,
                        CottonFileBrowserEntryType.File,
                        "old.txt",
                        "old.txt",
                        Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        "\"etag-old\"",
                        localUpdatedAtUtc: null,
                        sizeBytes: 12,
                        contentType: "text/plain"),
                ]);

            return new CottonDeviceToCloudSyncRunSummary(
                [
                    CottonDeviceToCloudSyncRootRunResult.SkippedDestructiveReviewRequired(root, plan),
                ]);
        }

        private static CottonBidirectionalSyncRunSummary CreateBidirectionalSummary(
            CottonCloudToDeviceSyncExecutionResult cloudExecutionResult,
            CottonDeviceToCloudSyncExecutionResult deviceExecutionResult)
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            CottonBidirectionalSyncExecutionPlan executionPlan = CreateBidirectionalExecutionPlan(root);

            return new CottonBidirectionalSyncRunSummary(
                [
                    CottonBidirectionalSyncRootRunResult.Completed(
                        root,
                        executionPlan,
                        cloudExecutionResult,
                        deviceExecutionResult),
                ]);
        }

        private static CottonBidirectionalSyncExecutionPlan CreateBidirectionalExecutionPlan(
            CottonSyncRootSnapshot root)
        {
            var preflightPlan = new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);

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

        private static CottonBidirectionalSyncRunSummary CreateBidirectionalDestructiveReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            var preflightPlan = new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
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
            var executionPlan = new CottonBidirectionalSyncExecutionPlan(
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

            return new CottonBidirectionalSyncRunSummary(
                [
                    CottonBidirectionalSyncRootRunResult.SkippedDestructiveReviewRequired(
                        root,
                        executionPlan),
                ]);
        }

        private static CottonBidirectionalSyncRunSummary CreateBidirectionalConflictReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            var preflightPlan = new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
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
            var executionPlan = new CottonBidirectionalSyncExecutionPlan(
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

            return new CottonBidirectionalSyncRunSummary(
                [
                    CottonBidirectionalSyncRootRunResult.SkippedConflictReviewRequired(
                        root,
                        executionPlan),
                ]);
        }

        private static CottonBidirectionalSyncRunSummary CreateBidirectionalBlockedReviewSummary()
        {
            CottonSyncRootSnapshot root = CreateBidirectionalRoot();
            var preflightPlan = new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
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
            var executionPlan = new CottonBidirectionalSyncExecutionPlan(
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

            return new CottonBidirectionalSyncRunSummary(
                [
                    CottonBidirectionalSyncRootRunResult.SkippedBlockedReviewRequired(
                        root,
                        executionPlan),
                ]);
        }

        private static CottonSyncRootSnapshot CreateCloudRoot()
        {
            return new CottonSyncRootSnapshot(
                CloudRootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-cloud-to-device",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);
        }

        private static CottonSyncRootSnapshot CreateDeviceRoot(Guid rootId)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Camera",
                    "Files / Camera"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/camera",
                    "Camera",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud);
        }

        private static CottonSyncRootSnapshot CreateBidirectionalRoot()
        {
            return new CottonSyncRootSnapshot(
                BidirectionalRootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/projects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.Bidirectional);
        }
    }
}
