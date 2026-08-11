using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncSettingsRunStatusTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncSettingsRunStatusTextTests
    {
        [Fact]
        public void Combined_status_reports_no_roots()
        {
            Assert.Equal("Syncing folders…", CottonSyncSettingsRunStatusText.StartingAllStatus);
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
                    confirmedUploadCount: 1,
                    refreshedCount: 0,
                    createdFolderCount: 1,
                    deletedLocalFileCount: 1,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 1),
                CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedLocalRoot(
                    CreateDeviceRoot(SkippedDeviceRootId)));

            Assert.Equal(
                "Sync complete. 1 downloaded, 2 uploaded, 1 upload confirmed, 1 original removed, "
                + "1 folder created, 1 blocked, 1 root skipped.",
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
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
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
        public void Single_root_status_reports_bidirectional_destructive_review_cancellation()
        {
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
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
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
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
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
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 0));

            Assert.Equal("Sync both ways", CottonBidirectionalSyncStatusText.ActionLabel);
            Assert.Equal("Sync needs a fresh account session.", CottonBidirectionalSyncStatusText.AccountUnavailableStatus);
            Assert.Equal("Offline. Sync needs internet.", CottonBidirectionalSyncStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync cancelled.", CottonBidirectionalSyncStatusText.CancelledStatus);
            Assert.Equal("Sync failed.", CottonBidirectionalSyncStatusText.FailedStatus);
            Assert.Equal("Syncing Projects both ways…", CottonBidirectionalSyncStatusText.CreateStartingStatus(" Projects "));
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
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 2,
                    blockedCount: 0));
            CottonDeviceToCloudSyncRunSummary blockedSummary = CreateDeviceSummary(
                new CottonDeviceToCloudSyncExecutionResult(
                    uploadedCount: 0,
                    confirmedUploadCount: 0,
                    refreshedCount: 0,
                    createdFolderCount: 0,
                    deletedLocalFileCount: 0,
                    deletedRemoteFileCount: 0,
                    removedManifestCount: 0,
                    skippedCount: 0,
                    blockedCount: 1));

            Assert.Equal("Upload new files", CottonDeviceToCloudSyncStatusText.ActionLabel);
            Assert.Equal(
                "Uploading new files from Camera…",
                CottonDeviceToCloudSyncStatusText.CreateStartingStatus(" Camera "));
            Assert.Equal("Offline. Sync needs internet.", CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync failed.", CottonDeviceToCloudSyncStatusText.FailedStatus);
            Assert.Equal(
                "Sync root is not configured to upload new files.",
                CottonDeviceToCloudSyncStatusText.UnsupportedDirectionStatus);
            Assert.Equal(
                "Sync complete. Everything is up to date.",
                CottonDeviceToCloudSyncStatusText.CreateCompletedStatus(summary));
            Assert.Equal(
                "Sync complete. 1 blocked.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(blockedSummary));
        }
    }
}
