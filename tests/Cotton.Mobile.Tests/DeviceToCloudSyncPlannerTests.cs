using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlannerTests
    {
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

    }
}
