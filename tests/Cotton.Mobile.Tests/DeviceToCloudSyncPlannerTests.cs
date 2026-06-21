using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlannerTests
    {
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid RemoteFolderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Planner_uploads_new_local_files()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadNewFile, item.Action);
            Assert.True(item.RequiresUpload);
            Assert.True(item.RequiresServerMutation);
            Assert.Null(item.CloudItemId);
            Assert.Null(item.ExpectedRemoteETag);
            Assert.Equal("alpha.txt", item.RelativePath);
            Assert.Equal("document-alpha", item.LocalSourceId);
            Assert.Equal(1, plan.UploadCount);
            Assert.True(plan.HasExecutableChanges);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_keeps_matching_manifest_file()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddSeconds(1), 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFile, item.Action);
            Assert.True(item.IsNoOp);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(1, plan.NoOpCount);
            Assert.False(plan.HasExecutableChanges);
        }

        [Fact]
        public void Planner_uploads_changed_manifest_file_when_remote_revision_matches()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddSeconds(3), 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadChangedFile, item.Action);
            Assert.True(item.RequiresUpload);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(1, plan.UploadCount);
        }

        [Fact]
        public void Planner_uploads_changed_manifest_file_when_size_changes()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 43));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadChangedFile, item.Action);
            Assert.True(item.RequiresUpload);
        }

        [Fact]
        public void Planner_blocks_changed_file_when_remote_revision_changed()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddSeconds(3), 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-2\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemoteRevisionChanged, item.Action);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
            Assert.Equal(1, plan.BlockedCount);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_blocks_remote_replacement_at_same_path_without_upload_or_delete()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddSeconds(1), 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(SecondFileId, "alpha.txt", "alpha.txt", "\"etag-2\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemoteTargetMissing, item.Action);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
            Assert.False(item.RequiresRemoteDelete);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(1, plan.BlockedCount);
            Assert.Equal(0, plan.RemoteDeleteCount);
        }

        [Fact]
        public void Planner_marks_missing_remote_etag_as_fresh_revision_needed()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddSeconds(3), 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", eTag: null));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
        }

        [Fact]
        public void Planner_deletes_remote_file_when_manifest_file_is_missing_locally()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                CreateLocalContent(),
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.DeleteRemoteFile, item.Action);
            Assert.True(item.RequiresRemoteDelete);
            Assert.True(item.IsDestructive);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(1, plan.RemoteDeleteCount);
            Assert.True(plan.HasDestructiveChanges);
        }

        [Fact]
        public void Planner_removes_manifest_orphan_when_remote_file_is_already_missing()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                CreateLocalContent(),
                CreateRemoteContent(),
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan, item.Action);
            Assert.True(item.RemovesManifestOnly);
            Assert.False(item.RequiresServerMutation);
            Assert.Equal(1, plan.ManifestRemovalCount);
        }

        [Fact]
        public void Planner_removes_manifest_only_when_remote_replacement_exists_for_missing_local_file()
        {
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(SecondFileId, "alpha.txt", "alpha.txt", "\"etag-2\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                CreateLocalContent(),
                remote,
                [manifest]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan, item.Action);
            Assert.True(item.RemovesManifestOnly);
            Assert.False(item.RequiresRemoteDelete);
            Assert.Equal(0, plan.RemoteDeleteCount);
            Assert.Equal(1, plan.ManifestRemovalCount);
            Assert.False(plan.HasDestructiveChanges);
        }

        [Fact]
        public void Planner_blocks_new_local_file_when_remote_path_exists()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemotePathConflict, item.Action);
            Assert.True(item.IsBlocked);
            Assert.Equal(FirstFileId, item.CloudItemId);
        }

        [Fact]
        public void Planner_blocks_invalid_local_problem_without_skipping_valid_siblings()
        {
            var invalidName = new CottonDeviceToCloudLocalProblemSnapshot(
                CottonDeviceToCloudLocalProblemKind.InvalidCloudName,
                CottonFileBrowserEntryType.File,
                "bad:name.txt",
                "bad:name.txt",
                "Name cannot be synced to the cloud.");
            var local = new CottonDeviceToCloudLocalContentSnapshot(
                "Projects",
                [CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42)],
                [invalidName]);

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                []);

            Assert.Equal(2, plan.Items.Count);
            CottonDeviceToCloudSyncPlanItem blocked =
                Assert.Single(plan.Items, item => item.Action == CottonDeviceToCloudSyncActionKind.BlockedLocalItemName);
            CottonDeviceToCloudSyncPlanItem upload =
                Assert.Single(plan.Items, item => item.Action == CottonDeviceToCloudSyncActionKind.UploadNewFile);
            Assert.True(blocked.IsBlocked);
            Assert.True(blocked.IsLocalProblem);
            Assert.Equal("bad:name.txt", blocked.DisplayName);
            Assert.Equal("bad:name.txt", blocked.RelativePath);
            Assert.True(upload.RequiresUpload);
            Assert.Equal(1, plan.BlockedCount);
            Assert.Equal(1, plan.LocalProblemCount);
            Assert.Equal(1, plan.UploadCount);
        }

        [Fact]
        public void Planner_creates_remote_folders_before_nested_uploads()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFolder("Nested", "Nested"),
                CreateLocalFile("alpha.txt", "Nested/alpha.txt", SyncedAt, 42));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                []);

            Assert.Equal(2, plan.Items.Count);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.CreateRemoteFolder, plan.Items[0].Action);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadNewFile, plan.Items[1].Action);
            Assert.True(plan.Items[0].RequiresRemoteFolderCreate);
            Assert.Equal(1, plan.RemoteFolderCreateCount);
            Assert.Equal(1, plan.UploadCount);
        }

        [Fact]
        public void Planner_keeps_existing_remote_folder()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFolder("Nested", "Nested"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFolder(RemoteFolderId, "Nested", "Nested"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFolder, item.Action);
            Assert.True(item.IsNoOp);
        }

        [Fact]
        public void Planner_rejects_wrong_roots_and_remote_content()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent();

            Assert.Throws<InvalidOperationException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(CreateCloudToDeviceRoot(), local, remote, []));

            Assert.Throws<InvalidOperationException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateRoot(CottonSyncDirection.DeviceToCloud, CottonSyncRootPermissionStatus.Unavailable),
                    local,
                    remote,
                    []));

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateReadyRoot(),
                    local,
                    new CottonDeviceToCloudRemoteContentSnapshot(
                        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        "Other",
                        []),
                    []));
        }

        [Fact]
        public void Planner_rejects_duplicate_paths_and_remote_file_ids()
        {
            CottonDeviceToCloudLocalContentSnapshot duplicateLocal = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42),
                CreateLocalFile("ALPHA.txt", "ALPHA.txt", SyncedAt, 43));
            CottonDeviceToCloudRemoteContentSnapshot duplicateRemotePath = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""),
                CreateRemoteFile(SecondFileId, "ALPHA.txt", "ALPHA.txt", "\"etag-2\""));
            CottonDeviceToCloudRemoteContentSnapshot duplicateRemoteId = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""),
                CreateRemoteFile(FirstFileId, "beta.txt", "beta.txt", "\"etag-2\""));
            CottonSyncedFileSnapshot manifest = CreateManifestFile(FirstFileId, "alpha.txt", "\"etag-1\"", SyncedAt);
            CottonSyncedFileSnapshot duplicateManifestPath = CreateManifestFile(
                SecondFileId,
                "ALPHA.txt",
                "\"etag-2\"",
                SyncedAt);
            CottonSyncedFileSnapshot duplicateManifestId = CreateManifestFile(
                FirstFileId,
                "beta.txt",
                "\"etag-2\"",
                SyncedAt);

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(CreateReadyRoot(), duplicateLocal, CreateRemoteContent(), []));

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateLocalContent(),
                    duplicateRemotePath,
                    []));

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateLocalContent(),
                    duplicateRemoteId,
                    []));

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateLocalContent(),
                    CreateRemoteContent(),
                    [manifest, duplicateManifestPath]));

            Assert.Throws<ArgumentException>(() =>
                CottonDeviceToCloudSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateLocalContent(),
                    CreateRemoteContent(),
                    [manifest, duplicateManifestId]));
        }

        private static CottonSyncRootSnapshot CreateReadyRoot()
        {
            return CreateRoot(CottonSyncDirection.DeviceToCloud, CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateCloudToDeviceRoot()
        {
            return CreateRoot(CottonSyncDirection.CloudToDevice, CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction,
            CottonSyncRootPermissionStatus permissionStatus)
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
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/primary%3AProjects",
                    "Projects",
                    permissionStatus),
                direction);
        }

        private static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            DateTime updatedAt,
            long sizeBytes,
            string? localSourceId = null)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                updatedAt,
                sizeBytes,
                "text/plain",
                localSourceId);
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFolder(string name, string relativePath)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFolder(name, relativePath, SyncedAt);
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
            string? eTag)
        {
            return new CottonDeviceToCloudRemoteItemSnapshot(
                CreateRemoteEntry(id, CottonFileBrowserEntryType.File, name, eTag),
                relativePath);
        }

        private static CottonDeviceToCloudRemoteItemSnapshot CreateRemoteFolder(
            Guid id,
            string name,
            string relativePath)
        {
            return new CottonDeviceToCloudRemoteItemSnapshot(
                CreateRemoteEntry(id, CottonFileBrowserEntryType.Folder, name, eTag: null),
                relativePath);
        }

        private static CottonFileBrowserEntry CreateRemoteEntry(
            Guid id,
            CottonFileBrowserEntryType type,
            string name,
            string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                type,
                name,
                type == CottonFileBrowserEntryType.File ? "Text" : "Folder",
                type == CottonFileBrowserEntryType.File ? "42 B · Text" : "Folder",
                type == CottonFileBrowserEntryType.File ? "More" : "Open",
                type == CottonFileBrowserEntryType.File ? "TXT" : "Folder",
                SyncedAt,
                type == CottonFileBrowserEntryType.File ? 42 : null,
                type == CottonFileBrowserEntryType.File ? "text/plain" : null,
                previewHashEncryptedHex: null,
                eTag);
        }

        private static CottonSyncedFileSnapshot CreateManifestFile(
            Guid id,
            string name,
            string eTag,
            DateTime syncedAt)
        {
            return new CottonSyncedFileSnapshot(
                id,
                name,
                eTag,
                SyncedAt,
                42,
                "text/plain",
                syncedAt);
        }
    }
}
