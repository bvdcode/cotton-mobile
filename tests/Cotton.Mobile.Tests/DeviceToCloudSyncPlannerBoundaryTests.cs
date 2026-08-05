using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlannerBoundaryTests
    {
        [Fact]
        public void Planner_includes_only_parent_folders_needed_by_pending_uploads()
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
        public void Planner_blocks_local_file_without_source_id()
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
        public void Planner_ignores_remote_only_files()
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

        [Fact]
        public void Planner_rejects_non_upload_only_direction()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent();

            Assert.Throws<InvalidOperationException>(() => CottonDeviceToCloudSyncPlanner.Create(
                CreateRoot(CottonSyncDirection.CloudToDevice),
                local,
                remote,
                []));
            Assert.Throws<InvalidOperationException>(() => CottonDeviceToCloudSyncPlanner.Create(
                CreateRoot(CottonSyncDirection.Bidirectional),
                local,
                remote,
                []));
        }
    }
}
