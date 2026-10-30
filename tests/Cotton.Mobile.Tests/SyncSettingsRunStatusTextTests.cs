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
        public void SingleRootSummaryUsesUploadStatus()
        {
            CottonDeviceToCloudSyncRunSummary summary = new([]);

            Assert.Equal(
                "No folders are set to sync.",
                CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(summary));
        }
    }
}
