using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlannerTests
    {
        [Fact]
        public void PlannerUploadsNewLocalFile()
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
        public void PlannerKeepsUploadedReceiptWhenRemoteIsMissingOrChanged()
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
        public void PlannerIgnoresReceiptWhenLocalFileIsMissing()
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
        public void PlannerDeletesLocalFileOnlyForUploadedReceiptAndDeleteRetention()
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
        public void PlannerBlocksLocalDeleteWhenUploadedRemoteRevisionIsMissingOrChanged()
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
        public void PlannerBlocksLocalDeleteForLegacyReceiptWithoutContentHash()
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
        public void PlannerRetriesPendingReceiptWithSameOperationIdWhenRemoteIsMissing()
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
        public void PlannerConfirmsPendingReceiptFromRemoteOperationMetadata()
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
        public void PlannerBlocksPendingConfirmationWhenRemoteSizeDiffers()
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
        public void PlannerBlocksPendingConfirmationWhenRemoteHashDiffers()
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
        public void PlannerBlocksPendingReceiptWhenLocalVersionOrPathChanges()
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
        public void PlannerKeepsUntrackedLocalFileWhenRemoteFileExists()
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
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFile, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.Equal("\"etag-1\"", item.ExpectedRemoteETag);
            Assert.True(item.IsNoOp);
            Assert.False(item.RequiresUpload);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void PlannerKeepsUntrackedLocalFileWhenRemoteContentDiffers()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FirstFileId,
                    "alpha.txt",
                    "alpha.txt",
                    "\"etag-1\"",
                    contentHash: TestContentHashes.Second));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.KeepExistingFile, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.True(item.IsNoOp);
            Assert.False(item.RequiresUpload);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void PlannerBlocksUntrackedLocalFileWhenRemotePathIsFolder()
        {
            CottonDeviceToCloudLocalContentSnapshot local = CreateLocalContent(
                CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt, 42, "document-alpha"));
            CottonFileBrowserEntry remoteFolder = CottonFileBrowserEntryFactory.CreateFolder(
                FirstFileId,
                "alpha.txt",
                SyncedAt);
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                new CottonDeviceToCloudRemoteItemSnapshot(remoteFolder, "alpha.txt"));

            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                local,
                remote,
                []);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.RemotePathConflict, item.Action);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresUpload);
        }
    }
}
