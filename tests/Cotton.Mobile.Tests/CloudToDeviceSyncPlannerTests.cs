using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPlannerTests
    {
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ThirdFileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Planner_downloads_new_remote_files()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.DownloadNewFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.Equal("\"etag-1\"", item.RemoteETag);
            Assert.Equal(1, plan.DownloadCount);
            Assert.True(plan.HasExecutableChanges);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_keeps_matching_local_file()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonSyncedFileSnapshot localFile = CottonSyncedFileSnapshot.Create(remoteFile, UpdatedAt.AddMinutes(1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.KeepExistingFile, item.Action);
            Assert.True(item.IsNoOp);
            Assert.Equal(1, plan.NoOpCount);
            Assert.False(plan.HasExecutableChanges);
        }

        [Fact]
        public void Planner_refreshes_file_when_remote_etag_changes()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "alpha.txt", "\"etag-2\"");
            var localFile = new CottonSyncedFileSnapshot(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt.AddMinutes(-5),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.Equal(1, plan.DownloadCount);
        }

        [Fact]
        public void Planner_renames_local_file_when_etag_matches_but_name_changes()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "renamed.txt", "\"etag-1\"");
            var localFile = new CottonSyncedFileSnapshot(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt.AddMinutes(1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RenameLocalFile, item.Action);
            Assert.True(item.RequiresLocalRename);
            Assert.Equal("renamed.txt", item.DisplayName);
            Assert.Equal(1, plan.LocalRenameCount);
        }

        [Fact]
        public void Planner_marks_missing_remote_etag_as_refresh_required()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", eTag: null));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_blocks_child_folders_until_recursive_sync_exists()
        {
            CottonFolderContent remote = CreateContent(
                CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""),
                CreateFolder("Archive"));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            Assert.Equal(2, plan.Items.Count);
            Assert.Contains(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.DownloadNewFile);
            Assert.Contains(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.BlockedFolder);
            Assert.True(plan.HasExecutableChanges);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_removes_local_orphans_from_manifest()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));
            var orphan = new CottonSyncedFileSnapshot(
                SecondFileId,
                "orphan.txt",
                "\"etag-old\"",
                UpdatedAt.AddDays(-1),
                100,
                "text/plain",
                UpdatedAt.AddHours(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                [orphan]);

            Assert.Equal(2, plan.Items.Count);
            CottonCloudToDeviceSyncPlanItem removal =
                Assert.Single(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan);
            Assert.Equal(SecondFileId, removal.TargetId);
            Assert.True(removal.RemovesLocalFile);
            Assert.Equal(1, plan.LocalRemovalCount);
        }

        [Fact]
        public void Planner_rejects_wrong_cloud_folder()
        {
            CottonFolderContent remote = new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "Other",
                [CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"")]);

            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(CreateReadyRoot(), remote, []));
        }

        [Fact]
        public void Planner_rejects_not_ready_root()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));

            Assert.Throws<InvalidOperationException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateRoot(CottonSyncRootPermissionStatus.Unavailable),
                    remote,
                    []));
        }

        [Fact]
        public void Planner_rejects_duplicate_manifest_or_remote_file_ids()
        {
            CottonSyncedFileSnapshot first = CottonSyncedFileSnapshot.Create(
                CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""),
                UpdatedAt);
            CottonSyncedFileSnapshot duplicate = new(
                FirstFileId,
                "duplicate.txt",
                "\"etag-2\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt);

            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(CreateReadyRoot(), CreateContent(), [first, duplicate]));

            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateContent(
                        CreateFile(ThirdFileId, "first.txt", "\"etag-1\""),
                        CreateFile(ThirdFileId, "second.txt", "\"etag-2\"")),
                    []));
        }

        private static CottonSyncRootSnapshot CreateReadyRoot()
        {
            return CreateRoot(CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncRootPermissionStatus permissionStatus)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    permissionStatus),
                CottonSyncDirection.CloudToDevice);
        }

        private static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(FolderId, "Projects", entries);
        }

        private static CottonFileBrowserEntry CreateFile(Guid id, string name, string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.File,
                name,
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }

        private static CottonFileBrowserEntry CreateFolder(string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.Folder,
                name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
