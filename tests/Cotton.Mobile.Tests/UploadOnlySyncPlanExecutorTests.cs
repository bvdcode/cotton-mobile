// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.UploadOnlySyncPlanExecutorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class UploadOnlySyncPlanExecutorTests
    {
        private static readonly string[] SuccessfulDeleteEvents =
            ["receipt:pending", "remote:upload", "receipt:uploaded", "local:delete"];
        private static readonly string[] SuccessfulUploadEvents =
            ["receipt:pending", "remote:upload", "receipt:uploaded"];
        private static readonly string[] RetryUploadEvents =
            ["receipt:load", "remote:upload", "receipt:uploaded"];
        private static readonly string[] ReceiptLoadEvents = ["receipt:load"];
        private static readonly string[] FailedUploadEvents = ["receipt:pending", "remote:upload"];
        private static readonly string[] LocalDeleteEvents = ["local:delete"];

        [Fact]
        public async Task ExecutePersistsPendingUploadsConfirmsAndDeletesInSafeOrder()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.LocalFileOperator.DeleteStatus = CottonDeviceToCloudLocalFileDeleteStatus.Deleted;

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem()));

            Assert.Equal(
                SuccessfulDeleteEvents,
                harness.Events);
            Assert.Equal(
                [CottonUploadReceiptStatus.Pending, CottonUploadReceiptStatus.Uploaded],
                harness.ReceiptStore.SaveHistory.Select(receipt => receipt.Status));
            CottonUploadReceiptSnapshot pending = harness.ReceiptStore.SaveHistory[0];
            CottonUploadReceiptSnapshot uploaded = harness.ReceiptStore.SaveHistory[1];
            Assert.NotEqual(Guid.Empty, pending.OperationId);
            Assert.Equal(pending.OperationId, uploaded.OperationId);
            Assert.Equal(RemoteFileId, uploaded.RemoteFileId);
            Assert.Equal(pending.OperationId, Assert.Single(harness.FileOperator.UploadCalls).UploadOperationId);
            Assert.Equal(pending.OperationId, Assert.Single(harness.LocalFileOperator.DeleteCalls).UploadOperationId);
            Assert.Equal(1, result.UploadedCount);
            Assert.Equal(1, result.DeletedLocalFileCount);
            Assert.Equal(0, result.BlockedCount);
        }

        [Fact]
        public async Task ExecuteKeepsOriginalWhenRetentionRequiresIt()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem()));

            Assert.Equal(
                SuccessfulUploadEvents,
                harness.Events);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
            Assert.Equal(1, result.UploadedCount);
            Assert.Equal(0, result.DeletedLocalFileCount);
        }

        [Fact]
        public async Task ExecuteRetryReusesMatchingOperationWithoutResavingPending()
        {
            CottonDeviceToCloudSyncPlanItem retryItem = CreateUploadItem(OperationId);
            CottonUploadReceiptSnapshot pending = CottonUploadReceiptSnapshot.CreatePending(
                retryItem,
                OperationId,
                RecordedAt.AddMinutes(-1));
            ExecutionHarness harness = new(
                CottonUploadOriginalRetention.KeepOriginals,
                [pending]);

            await harness.Executor.ExecuteAsync(InstanceUri, harness.Root, CreatePlan(retryItem));

            Assert.Equal(
                RetryUploadEvents,
                harness.Events);
            Assert.Equal(0, harness.ReceiptStore.PendingSaveCount);
            Assert.Equal(1, harness.ReceiptStore.UploadedSaveCount);
            Assert.Equal(OperationId, Assert.Single(harness.FileOperator.UploadCalls).UploadOperationId);
            Assert.Equal(OperationId, Assert.Single(harness.ReceiptStore.Receipts).OperationId);
        }

        [Fact]
        public async Task ExecuteRetryRejectsMissingMatchingPendingReceiptBeforeUpload()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);

            await Assert.ThrowsAsync<InvalidDataException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem(OperationId))));

            Assert.Equal(ReceiptLoadEvents, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
            Assert.Equal(0, harness.ReceiptStore.PendingSaveCount);
            Assert.Equal(0, harness.ReceiptStore.UploadedSaveCount);
        }

        [Fact]
        public async Task ExecuteRetryRejectsPendingReceiptForDifferentContent()
        {
            CottonDeviceToCloudSyncPlanItem staleItem = CreateUploadItem(OperationId, TestContentHashes.Second);
            CottonUploadReceiptSnapshot staleReceipt = CottonUploadReceiptSnapshot.CreatePending(
                staleItem,
                OperationId,
                RecordedAt.AddMinutes(-1));
            ExecutionHarness harness = new(
                CottonUploadOriginalRetention.KeepOriginals,
                [staleReceipt]);

            await Assert.ThrowsAsync<InvalidDataException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem(OperationId))));

            Assert.Equal(ReceiptLoadEvents, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
        }

        [Fact]
        public async Task ExecuteUploadFailureLeavesPendingReceiptAndDoesNotDelete()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.FileOperator.UploadException = new IOException("Upload failed.");

            await Assert.ThrowsAsync<IOException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem())));

            Assert.Equal(FailedUploadEvents, harness.Events);
            CottonUploadReceiptSnapshot receipt = Assert.Single(harness.ReceiptStore.Receipts);
            Assert.True(receipt.IsPending);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
        }

        [Fact]
        public async Task ExecuteUploadedReceiptFailurePreventsOriginalDelete()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.ReceiptStore.ThrowOnUploadedSave = true;

            await Assert.ThrowsAsync<IOException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem())));

            Assert.Equal(
                SuccessfulUploadEvents,
                harness.Events);
            CottonUploadReceiptSnapshot receipt = Assert.Single(harness.ReceiptStore.Receipts);
            Assert.True(receipt.IsPending);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
        }

        [Theory]
        [InlineData(CottonUploadOriginalRetention.KeepOriginals, false)]
        [InlineData(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload, true)]
        public async Task ExecuteConfirmsPendingWithoutUploadThenOptionallyDeletesOriginal(
            CottonUploadOriginalRetention retention,
            bool expectsDelete)
        {
            CottonUploadReceiptSnapshot pending = CottonUploadReceiptSnapshot.CreatePending(
                CreateUploadItem(OperationId),
                OperationId,
                RecordedAt.AddMinutes(-1));
            ExecutionHarness harness = new(retention, [pending]);

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateConfirmationItem()));

            string[] expectedEvents = expectsDelete
                ? ["receipt:uploaded", "local:delete"]
                : ["receipt:uploaded"];
            Assert.Equal(expectedEvents, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
            CottonUploadReceiptSnapshot receipt = Assert.Single(harness.ReceiptStore.Receipts);
            Assert.True(receipt.IsUploaded);
            Assert.Equal(OperationId, receipt.OperationId);
            Assert.Equal(RemoteFileId, receipt.RemoteFileId);
            Assert.Equal(1, result.ConfirmedUploadCount);
            Assert.Equal(expectsDelete ? 1 : 0, result.DeletedLocalFileCount);
        }

        [Theory]
        [InlineData(CottonDeviceToCloudLocalFileDeleteStatus.Deleted, 1, 0, 0)]
        [InlineData(CottonDeviceToCloudLocalFileDeleteStatus.AlreadyMissing, 0, 1, 0)]
        [InlineData(CottonDeviceToCloudLocalFileDeleteStatus.Changed, 0, 0, 1)]
        [InlineData(CottonDeviceToCloudLocalFileDeleteStatus.Unsupported, 0, 0, 1)]
        public async Task ExecuteCountsStandaloneCleanupStatus(
            CottonDeviceToCloudLocalFileDeleteStatus deleteStatus,
            int expectedDeleted,
            int expectedSkipped,
            int expectedBlocked)
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.LocalFileOperator.DeleteStatus = deleteStatus;

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateCleanupItem()));

            Assert.Equal(LocalDeleteEvents, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
            Assert.Empty(harness.ReceiptStore.SaveHistory);
            Assert.Equal(expectedDeleted, result.DeletedLocalFileCount);
            Assert.Equal(expectedSkipped, result.SkippedCount);
            Assert.Equal(expectedBlocked, result.BlockedCount);
        }

        [Theory]
        [InlineData(CottonDeviceToCloudSyncActionKind.UploadChangedFile)]
        [InlineData(CottonDeviceToCloudSyncActionKind.DeleteRemoteFile)]
        [InlineData(CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan)]
        public async Task ExecuteRejectsTwoWayMirrorActions(CottonDeviceToCloudSyncActionKind action)
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);
            CottonDeviceToCloudSyncPlanItem item = new(
                action,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                RemoteFileId,
                "etag-remote",
                LocalUpdatedAt,
                42,
                "image/jpeg",
                "primary:DCIM/Camera/photo.jpg",
                uploadOperationId: null,
                TestContentHashes.First);

            await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(item)));

            Assert.Empty(harness.Events);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteRejectsPlanWithDifferentRootOrFolder(bool changesRootId)
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);
            CottonDeviceToCloudSyncPlanSnapshot plan = new(
                changesRootId ? Guid.NewGuid() : SyncRootId,
                changesRootId ? RootFolderId : Guid.NewGuid(),
                "Camera",
                [CreateUploadItem()]);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                harness.Executor.ExecuteAsync(InstanceUri, harness.Root, plan));

            Assert.Empty(harness.Events);
        }
    }
}
