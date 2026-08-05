using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.BidirectionalSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncPlannerBoundaryTests
    {
        [Fact]
        public void Local_delete_requires_remote_delete_review_when_remote_is_unchanged()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "notes.txt", "notes.txt", "\"etag-1\"", sizeBytes: 42));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                CreateLocalContent(),
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.DeleteRemoteFile, item.Action);
            Assert.True(item.RequiresRemoteDelete);
            Assert.True(item.IsDestructive);
            Assert.Equal(1, plan.RemoteDeleteCount);
            Assert.True(plan.HasDestructiveChanges);
        }

        [Fact]
        public void Remote_new_file_downloads_when_local_path_is_empty()
        {
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(NewRemoteFileId, "remote.txt", "remote.txt", "\"etag-new\"", sizeBytes: 7));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                CreateLocalContent(),
                remote,
                []);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.DownloadNewFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.False(item.IsBlocked);
        }

        [Fact]
        public void New_local_file_blocks_when_remote_path_already_exists()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(NewRemoteFileId, "notes.txt", "notes.txt", "\"etag-remote\"", sizeBytes: 7));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                []);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.RemotePathConflict, item.Action);
            Assert.True(item.IsConflict);
            Assert.True(item.IsBlocked);
        }

        [Fact]
        public void Planner_requires_bidirectional_ready_root()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent();
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent();

            Assert.Throws<InvalidOperationException>(
                () => CottonBidirectionalSyncPlanner.Create(
                    CreateRoot(CottonSyncDirection.DeviceToCloud),
                    local,
                    remote,
                    []));
            Assert.Throws<InvalidOperationException>(
                () => CottonBidirectionalSyncPlanner.Create(
                    CreateRoot(CottonSyncDirection.Bidirectional, CottonSyncRootPermissionStatus.Unavailable),
                    local,
                    remote,
                    []));
        }
    }
}
