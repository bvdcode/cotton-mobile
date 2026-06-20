using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncPlannerTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid NewRemoteFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly DateTime RemoteUpdatedAt = new(2026, 6, 20, 18, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 18, 5, 0, 0, DateTimeKind.Utc);

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
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 84, updatedAtUtc: SyncedAt.AddMinutes(1)));
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
            Assert.False(item.IsBlocked);
            Assert.Equal(1, plan.UploadCount);
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
        public void Local_and_remote_change_same_file_blocks_as_conflict()
        {
            CottonSyncedFileSnapshot manifest = CreateManifest("\"etag-1\"", sizeBytes: 42);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("notes.txt", "notes.txt", sizeBytes: 84, updatedAtUtc: SyncedAt.AddMinutes(1)));
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

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction = CottonSyncDirection.Bidirectional,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/projects",
                    "Projects",
                    permissionStatus),
                direction);
        }

        private static CottonSyncedFileSnapshot CreateManifest(string eTag, long sizeBytes)
        {
            return new CottonSyncedFileSnapshot(
                FileId,
                "notes.txt",
                eTag,
                RemoteUpdatedAt,
                sizeBytes,
                "text/plain",
                SyncedAt,
                "notes.txt");
        }

        private static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            long sizeBytes,
            DateTime updatedAtUtc)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                updatedAtUtc,
                sizeBytes,
                "text/plain",
                $"local:{relativePath}");
        }

        private static CottonDeviceToCloudRemoteContentSnapshot CreateRemoteContent(
            params CottonDeviceToCloudRemoteItemSnapshot[] items)
        {
            return new CottonDeviceToCloudRemoteContentSnapshot(FolderId, "Projects", items);
        }

        private static CottonDeviceToCloudRemoteItemSnapshot CreateRemoteFile(
            Guid id,
            string name,
            string relativePath,
            string? eTag,
            long sizeBytes)
        {
            return new CottonDeviceToCloudRemoteItemSnapshot(
                CottonFileBrowserEntry.CreateCached(
                    id,
                    CottonFileBrowserEntryType.File,
                    name,
                    "Text",
                    $"{sizeBytes} B · Text",
                    "More",
                    "TXT",
                    RemoteUpdatedAt,
                    sizeBytes,
                    "text/plain",
                    previewHashEncryptedHex: null,
                    eTag),
                relativePath);
        }
    }
}
