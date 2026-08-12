using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.BidirectionalSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncPlannerTests
    {
        [Fact]
        public void Remote_only_change_refreshes_local_file_without_upload_conflict()
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
        public void Local_only_change_uploads_file_with_expected_remote_revision()
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
        public void Same_size_and_timestamp_content_change_uploads_file()
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
        public void Remote_only_rename_renames_local_file_without_download()
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
        public void Remote_deletion_carries_reviewed_local_content_hash()
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
        public void Remote_recreated_same_path_refreshes_unchanged_local_file()
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
        public void Remote_recreated_same_path_blocks_when_local_file_changed()
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
        public void Remote_recreated_same_path_without_etag_requires_fresh_revision()
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
        public void Local_and_remote_change_same_file_blocks_as_conflict()
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
        public void Same_size_and_timestamp_content_change_blocks_when_remote_also_changed()
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
