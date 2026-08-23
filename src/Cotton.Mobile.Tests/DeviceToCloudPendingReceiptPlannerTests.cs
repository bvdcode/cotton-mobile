using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudPendingReceiptPlannerTests
    {
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
        public void PlannerConfirmsCompletedPendingOperationBeforeReportingNewLocalVersion()
        {
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();
            CottonDeviceToCloudLocalContentSnapshot changedLocal = CreateLocalContent(
                CreateLocalFile(
                    "alpha.txt",
                    "alpha.txt",
                    SyncedAt.AddMinutes(2),
                    84,
                    "document-alpha",
                    TestContentHashes.Second));
            CottonDeviceToCloudRemoteContentSnapshot remote = CreateRemoteContent(
                CreateRemoteFile(
                    FirstFileId,
                    "alpha.txt",
                    "alpha.txt",
                    "\"etag-1\"",
                    OperationId));

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(),
                changedLocal,
                remote,
                [receipt]).Items);

            Assert.Equal(CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload, item.Action);
            Assert.Equal(OperationId, item.UploadOperationId);
            Assert.Equal(FirstFileId, item.CloudItemId);
        }

        [Theory]
        [InlineData(41, TestContentHashes.First)]
        [InlineData(42, TestContentHashes.Second)]
        public void PlannerBlocksPendingConfirmationWhenRemoteContentDiffers(
            long remoteSize,
            string remoteHash)
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
                    remoteSize,
                    remoteHash));

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
    }
}
