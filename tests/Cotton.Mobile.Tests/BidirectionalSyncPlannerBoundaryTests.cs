using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.BidirectionalSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncPlannerBoundaryTests
    {
        [Fact]
        public void LocalDeleteRequiresRemoteDeleteReviewWhenRemoteIsUnchanged()
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
        public void RemoteNewFileDownloadsWhenLocalPathIsEmpty()
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
        public void NewLocalFileBlocksWhenRemotePathAlreadyExists()
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
        public void PlannerRequiresBidirectionalReadyRoot()
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
