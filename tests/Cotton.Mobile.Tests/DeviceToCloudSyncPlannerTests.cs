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
        private static readonly Guid OperationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Planner_uploads_new_local_file()
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
            Assert.Equal("alpha.txt", item.RelativePath);
            Assert.Equal("document-alpha", item.LocalSourceId);
            Assert.Null(item.UploadOperationId);
            Assert.True(item.RequiresUpload);
            Assert.Equal(1, plan.UploadCount);
            Assert.True(plan.HasExecutableChanges);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void Planner_keeps_uploaded_receipt_when_remote_is_missing_or_changed()
        {
            CottonUploadReceiptSnapshot receipt = CreateUploadedReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot changedRemote = CreateRemoteContent(
                CreateRemoteFile(SecondFileId, "alpha.txt", "alpha.txt", "\"etag-2\""));

            CottonDeviceToCloudSyncPlanSnapshot missingRemotePlan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                [receipt]);
            CottonDeviceToCloudSyncPlanSnapshot changedRemotePlan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                changedRemote,
                [receipt]);

            AssertUploadedReceiptIsKept(missingRemotePlan);
            AssertUploadedReceiptIsKept(changedRemotePlan);
        }

        [Fact]
        public void Planner_ignores_receipt_when_local_file_is_missing()
        {
            CottonUploadReceiptSnapshot receipt = CreateUploadedReceipt();

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                CreateLocalContent(),
                CreateRemoteContent(),
                [receipt]);

            Assert.Empty(plan.Items);
            Assert.False(plan.HasExecutableChanges);
        }

        [Fact]
        public void Planner_deletes_local_file_only_for_uploaded_receipt_and_delete_retention()
        {
            CottonUploadReceiptSnapshot receipt = CreateUploadedReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload),
                local,
                CreateRemoteContent(
                    CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\"")),
                [receipt]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.True(item.RequiresLocalDelete);
            Assert.False(item.RequiresUpload);
            Assert.Equal(1, plan.LocalDeleteCount);
            Assert.True(plan.HasExecutableChanges);
        }

        [Fact]
        public void Planner_blocks_local_delete_when_uploaded_remote_revision_is_missing_or_changed()
        {
            CottonUploadReceiptSnapshot receipt = CreateUploadedReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonSyncRootSnapshot root = CreateReadyRoot(
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);

            CottonDeviceToCloudSyncPlanSnapshot missingRemotePlan = CottonDeviceToCloudSyncPlanner.Create(
                root,
                local,
                CreateRemoteContent(),
                [receipt]);
            CottonDeviceToCloudSyncPlanSnapshot changedRemotePlan = CottonDeviceToCloudSyncPlanner.Create(
                root,
                local,
                CreateRemoteContent(
                    CreateRemoteFile(SecondFileId, "alpha.txt", "alpha.txt", "\"etag-2\"")),
                [receipt]);

            AssertUploadedReceiptDeleteIsBlocked(missingRemotePlan);
            AssertUploadedReceiptDeleteIsBlocked(changedRemotePlan);
        }

        [Fact]
        public void Planner_blocks_local_delete_for_legacy_receipt_without_content_hash()
        {
            CottonUploadReceiptSnapshot receipt = CreateUploadedReceipt(contentHash: null);
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload),
                local,
                CreateRemoteContent(
                    CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\"")),
                [receipt]);

            AssertPendingLocalChangeIsBlocked(plan);
        }

        [Fact]
        public void Planner_retries_pending_receipt_with_same_operation_id_when_remote_is_missing()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                CreateRemoteContent(),
                [receipt]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadNewFile, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.Equal("document-alpha", item.LocalSourceId);
            Assert.True(item.RequiresUpload);
            Assert.Equal(1, plan.UploadCount);
        }

        [Fact]
        public void Planner_confirms_pending_receipt_from_remote_operation_metadata()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FirstFileId,
                    "alpha.txt",
                    "alpha.txt",
                    "\"etag-1\"",
                    OperationId));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [receipt]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.True(item.ConfirmsPendingUpload);
            Assert.Equal(1, plan.ConfirmedUploadCount);
            Assert.True(plan.HasExecutableChanges);
        }

        [Fact]
        public void Planner_blocks_pending_confirmation_when_remote_size_differs()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FirstFileId,
                    "alpha.txt",
                    "alpha.txt",
                    "\"etag-1\"",
                    OperationId,
                    sizeBytes: 41));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [receipt]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
        }

        [Fact]
        public void Planner_blocks_pending_confirmation_when_remote_hash_differs()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FirstFileId,
                    "alpha.txt",
                    "alpha.txt",
                    "\"etag-1\"",
                    OperationId,
                    contentHash: TestContentHashes.Second));

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                [receipt]).Items);

            Assert.Equal(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
        }

        [Fact]
        public void Planner_blocks_pending_receipt_when_local_version_or_path_changes()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot changedVersion = CreateLocalContent(
                CreateLocalFile(
                    "alpha.txt",
                    "alpha.txt",
                    SyncedAt,
                    42,
                    "document-alpha",
                    TestContentHashes.Second));
            CottonDeviceToCloudLocalContentSnapshot changedPath = CreateLocalContent(
                CreateLocalFile("renamed.txt", "renamed.txt", SyncedAt, 42, "document-alpha"));

            CottonDeviceToCloudSyncPlanSnapshot changedVersionPlan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                changedVersion,
                CreateRemoteContent(),
                [receipt]);
            CottonDeviceToCloudSyncPlanSnapshot changedPathPlan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                changedPath,
                CreateRemoteContent(),
                [receipt]);

            AssertPendingLocalChangeIsBlocked(changedVersionPlan);
            AssertPendingLocalChangeIsBlocked(changedPathPlan);
        }

        [Fact]
        public void Planner_blocks_new_local_file_when_remote_path_exists()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemotePathConflict, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
        }

        [Fact]
        public void Planner_includes_only_parent_folders_needed_by_pending_uploads()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt(
                relativePath: "Pending/alpha.txt");
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

        private static void AssertUploadedReceiptIsKept(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFile, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.True(item.IsNoOp);
            Assert.False(plan.HasExecutableChanges);
        }

        private static void AssertPendingLocalChangeIsBlocked(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
            Assert.True(plan.HasBlockingItems);
        }

        private static void AssertUploadedReceiptDeleteIsBlocked(CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.False(item.RequiresLocalDelete);
            Assert.True(item.IsBlocked);
        }

        private static CottonSyncRootSnapshot CreateReadyRoot(
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals)
        {
            return CreateRoot(CottonSyncDirection.DeviceToCloud, retention);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncDirection direction,
            CottonUploadOriginalRetention retention = CottonUploadOriginalRetention.KeepOriginals)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                direction,
                retention);
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
            string? localSourceId,
            string contentHash = TestContentHashes.First)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                updatedAt,
                sizeBytes,
                "text/plain",
                localSourceId,
                contentHash);
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
            string eTag,
            Guid? operationId = null,
            long sizeBytes = 42,
            string contentHash = TestContentHashes.First)
        {
            IReadOnlyDictionary<string, string>? metadata = operationId.HasValue
                ? new Dictionary<string, string>
                {
                    [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.Value.ToString("N"),
                }
                : null;
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.CreateFile(
                id,
                name,
                SyncedAt,
                sizeBytes,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag,
                metadata,
                contentHash);
            return new CottonDeviceToCloudRemoteItemSnapshot(entry, relativePath);
        }

        private static CottonUploadReceiptSnapshot CreatePendingReceipt(
            string relativePath = "alpha.txt")
        {
            return new CottonUploadReceiptSnapshot(
                "document-alpha",
                relativePath,
                SyncedAt,
                42,
                "text/plain",
                OperationId,
                CottonUploadReceiptStatus.Pending,
                SyncedAt.AddMinutes(1),
                remoteFileId: null,
                remoteETag: null,
                TestContentHashes.First);
        }

        private static CottonUploadReceiptSnapshot CreateUploadedReceipt(
            string? contentHash = TestContentHashes.First)
        {
            return new CottonUploadReceiptSnapshot(
                "document-alpha",
                "alpha.txt",
                SyncedAt,
                42,
                "text/plain",
                OperationId,
                CottonUploadReceiptStatus.Uploaded,
                SyncedAt.AddMinutes(1),
                FirstFileId,
                "\"etag-1\"",
                contentHash);
        }
    }
}
