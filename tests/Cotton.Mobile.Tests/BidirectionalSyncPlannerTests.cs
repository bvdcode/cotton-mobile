using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.BidirectionalSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncPlannerTests
    {
        [Fact]
        public void RemoteOnlyChangeRefreshesLocalFileWithoutUploadConflict()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt.AddSeconds(-1)));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "notes.txt", "notes.txt", "\"etag-2\"", sizeBytes: 99));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.RefreshLocalFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.False(item.RequiresUpload);
            Assert.False(item.IsBlocked);
            Assert.Equal(1, plan.DownloadCount);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void LocalOnlyChangeUploadsFileWithExpectedRemoteRevision()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile(
                    "notes.txt",
                    "notes.txt",
                    sizeBytes: 84,
                    updatedAtUtc: SyncedAt.AddMinutes(1),
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "notes.txt", "notes.txt", "\"etag-1\"", sizeBytes: 42));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.UploadChangedFile, item.Action);
            Assert.True(item.RequiresUpload);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal("local:notes.txt", item.LocalSourceId);
            Assert.False(item.IsBlocked);
            Assert.Equal(1, plan.UploadCount);
        }

        [Fact]
        public void SameSizeAndTimestampContentChangeUploadsFile()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile(
                    "notes.txt",
                    "notes.txt",
                    sizeBytes: 42,
                    updatedAtUtc: SyncedAt,
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "notes.txt", "notes.txt", "\"etag-1\"", sizeBytes: 42));

            CottonBidirectionalSyncPlanItem item = Assert.Single(CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]).Items);

            Assert.Equal(CottonBidirectionalSyncActionKind.UploadChangedFile, item.Action);
            Assert.Equal(TestContentHashes.Second, item.LocalContentHash);
        }

        [Fact]
        public void RemoteOnlyRenameRenamesLocalFileWithoutDownload()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt.AddSeconds(-1)));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "renamed.txt", "renamed.txt", "\"etag-1\"", sizeBytes: 42));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.RenameLocalFile, item.Action);
            Assert.True(item.RequiresLocalRename);
            Assert.Equal("notes.txt", item.PreviousRelativePath);
            Assert.Equal("renamed.txt", item.RelativePath);
            Assert.Equal(1, plan.LocalRenameCount);
            Assert.False(item.RequiresDownload);
            Assert.False(item.IsBlocked);
        }

        [Fact]
        public void RemoteDeletionCarriesReviewedLocalContentHash()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                CreateRemoteContent(),
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.RemoveLocalFile, item.Action);
            Assert.Equal(manifest.ContentHash, item.LocalContentHash);
            Assert.True(item.IsDestructive);
        }

        [Fact]
        public void RemoteRecreatedSamePathRefreshesUnchangedLocalFile()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt.AddSeconds(-1)));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(NewRemoteFileId, "notes.txt", "notes.txt", "\"etag-new\"", sizeBytes: 99));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.RefreshLocalFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.False(item.IsDestructive);
            Assert.False(item.IsBlocked);
            Assert.Equal(NewRemoteFileId, item.CloudItemId);
            Assert.Equal("\"etag-new\"", item.ExpectedRemoteETag);
            Assert.Equal(1, plan.DownloadCount);
            Assert.Equal(0, plan.LocalDeleteCount);
        }

        [Fact]
        public void RemoteRecreatedSamePathBlocksWhenLocalFileChanged()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile(
                    "notes.txt",
                    "notes.txt",
                    sizeBytes: 84,
                    updatedAtUtc: SyncedAt.AddMinutes(1),
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(NewRemoteFileId, "notes.txt", "notes.txt", "\"etag-new\"", sizeBytes: 99));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.FileChangedOnBothSides, item.Action);
            Assert.True(item.IsConflict);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresDownload);
            Assert.False(item.RequiresUpload);
            Assert.Equal(NewRemoteFileId, item.CloudItemId);
        }

        [Fact]
        public void RemoteRecreatedSamePathWithoutEtagRequiresFreshRevision()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 42, updatedAtUtc: SyncedAt.AddSeconds(-1)));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(NewRemoteFileId, "notes.txt", "notes.txt", eTag: null, sizeBytes: 99));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresDownload);
            Assert.Equal(NewRemoteFileId, item.CloudItemId);
        }

        [Fact]
        public void LocalAndRemoteChangeSameFileBlocksAsConflict()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile(
                    "notes.txt",
                    "notes.txt",
                    sizeBytes: 84,
                    updatedAtUtc: SyncedAt.AddMinutes(1),
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FileId, "notes.txt", "notes.txt", "\"etag-2\"", sizeBytes: 99));

            CottonBidirectionalSyncPlanSnapshot plan = CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]);

            CottonBidirectionalSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonBidirectionalSyncActionKind.FileChangedOnBothSides, item.Action);
            Assert.True(item.IsConflict);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresDownload);
            Assert.False(item.RequiresUpload);
            Assert.Equal(1, plan.ConflictCount);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void SameSizeAndTimestampContentChangeBlocksWhenRemoteAlsoChanged()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile(
                    "notes.txt",
                    "notes.txt",
                    sizeBytes: 42,
                    updatedAtUtc: SyncedAt,
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FileId,
                    "notes.txt",
                    "notes.txt",
                    "\"etag-2\"",
                    sizeBytes: 42,
                    TestContentHashes.Third));

            CottonBidirectionalSyncPlanItem item = Assert.Single(CottonBidirectionalSyncPlanner.Create(
                CreateRoot(),
                local,
                remote,
                [manifest]).Items);

            Assert.Equal(CottonBidirectionalSyncActionKind.FileChangedOnBothSides, item.Action);
            Assert.True(item.IsBlocked);
            Assert.Equal(TestContentHashes.Second, item.LocalContentHash);
            Assert.Equal(TestContentHashes.Third, item.RemoteContentHash);
        }
    }
}
