using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncSettingsRunStatusTextTests
    {
        [Fact]
        public void EmptySummaryReportsNoConfiguredFolders()
        {
            string status = CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                new CottonDeviceToCloudSyncRunSummary([]));

            Assert.Equal("No folders are set to sync.", status);
        }

        [Fact]
        public void CompletedSummaryReportsUploadResults()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonDeviceToCloudSyncPlanSnapshot plan = new(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);
            CottonDeviceToCloudSyncExecutionResult execution = new(
                uploadedCount: 2,
                confirmedUploadCount: 1,
                createdFolderCount: 1,
                deletedLocalFileCount: 0,
                skippedCount: 0,
                blockedCount: 0);
            CottonDeviceToCloudSyncRunSummary summary = new(
                [CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, execution)]);

            string status = CottonSyncSettingsRunStatusText.CreateCompletedStatus(summary);

            Assert.Equal("Sync complete. 2 uploaded, 1 upload confirmed, 1 folder created.", status);
        }

        [Fact]
        public void PausedSingleRootReportsPauseInsteadOfCompletion()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonDeviceToCloudSyncRunSummary summary = new(
                [CottonDeviceToCloudSyncRootRunResult.SkippedPaused(root)]);

            Assert.Equal(
                $"Paused syncing {root.CloudFolder.FolderName}.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(summary));
        }

        [Theory]
        [InlineData(0, 3, 0, "3 items need attention")]
        [InlineData(2, 1, 0, "2 uploaded, 1 item needs attention")]
        [InlineData(2, 0, 1, "2 uploaded, 1 folder failed")]
        public void IncompleteSummaryDoesNotReportSuccess(int uploaded, int blocked, int failedRoots, string counts)
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonDeviceToCloudSyncPlanSnapshot plan = new(
                root.Id, root.CloudFolder.FolderId, root.CloudFolder.FolderName, []);
            CottonDeviceToCloudSyncExecutionResult execution = new(uploaded, 0, 0, 0, 0, blocked);
            CottonDeviceToCloudSyncRunSummary summary = new(
                [CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, execution)]);

            Assert.Equal(
                $"Sync incomplete. {counts}. Review the folders below.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(summary, failedRoots));
        }

        [Fact]
        public void AllFailedRootsDoNotReportEmptyConfigurationOrSuccess()
        {
            Assert.Equal(
                "Sync incomplete. 2 folders failed. Review the folders below.",
                CottonSyncSettingsRunStatusText.CreateCompletedStatus(new CottonDeviceToCloudSyncRunSummary([]), 2));
        }

        [Fact]
        public void SkippedRootDoesNotReportSuccess()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonDeviceToCloudSyncRunSummary summary = new(
                [CottonDeviceToCloudSyncRootRunResult.SkippedNotReady(root)]);

            Assert.StartsWith("Sync incomplete.", CottonSyncSettingsRunStatusText.CreateCompletedStatus(summary));
        }

        [Fact]
        public void SingleRootSummaryUsesUploadStatus()
        {
            CottonDeviceToCloudSyncRunSummary summary = new([]);

            Assert.Equal(
                "No folders are set to sync.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(summary));
        }
    }
}
