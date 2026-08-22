using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlannerBoundaryTests
    {
        [Fact]
        public void PlannerIncludesOnlyParentFoldersNeededByPendingUploads()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt("Pending/alpha.txt");
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFolder("Pending", "Pending"),
                CreateLocalFile("alpha.txt", "Pending/alpha.txt", SyncedAt, 42, "document-alpha"),
                CreateLocalFolder("Unused", "Unused"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                [receipt]);

            Assert.Equal(2, plan.Items.Count);
            CottonDeviceToCloudSyncPlanItem folder = plan.Items[0];
            CottonDeviceToCloudSyncPlanItem upload = plan.Items[1];
            Assert.Equal(CottonDeviceToCloudSyncActionKind.CreateRemoteFolder, folder.Action);
            Assert.Equal("Pending", folder.RelativePath);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadNewFile, upload.Action);
            Assert.Equal("Pending/alpha.txt", upload.RelativePath);
            Assert.Equal(OperationId, upload.UploadOperationId);
            Assert.DoesNotContain(plan.Items, item => item.RelativePath == "Unused");
        }

        [Fact]
        public void PlannerBlocksLocalFileWithoutSourceId()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, localSourceId: null));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.BlockedLocalSource, item.Action);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
            Assert.Equal(1, plan.BlockedCount);
        }

        [Fact]
        public void PlannerIgnoresRemoteOnlyFiles()
        {
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                CreateLocalContent(),
                remote,
                []);

            Assert.Empty(plan.Items);
            Assert.False(plan.HasExecutableChanges);
            Assert.False(plan.HasBlockingItems);
        }
    }
}
