using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncSettingsRunStatusTextTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid CloudRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid DeviceRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid SkippedDeviceRootId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Combined_status_reports_no_roots()
        {
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
    }
}
