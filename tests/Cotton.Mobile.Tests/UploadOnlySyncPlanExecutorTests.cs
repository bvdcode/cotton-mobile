// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.UploadOnlySyncPlanExecutorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class UploadOnlySyncPlanExecutorTests
    {
        [Fact]
        public async Task Execute_persists_pending_uploads_confirms_and_deletes_in_safe_order()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.LocalFileOperator.DeleteStatus = CottonDeviceToCloudLocalFileDeleteStatus.Deleted;

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem()));

            Assert.Equal(
                new[] { "receipt:pending", "remote:upload", "receipt:uploaded", "local:delete" },
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
        public async Task Execute_keeps_original_when_retention_requires_it()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);

            CottonDeviceToCloudSyncExecutionResult result = await harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem()));

            Assert.Equal(
                new[] { "receipt:pending", "remote:upload", "receipt:uploaded" },
                harness.Events);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
            Assert.Equal(1, result.UploadedCount);
            Assert.Equal(0, result.DeletedLocalFileCount);
        }

        [Fact]
        public async Task Execute_retry_reuses_matching_operation_without_resaving_pending()
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
                new[] { "receipt:load", "remote:upload", "receipt:uploaded" },
                harness.Events);
            Assert.Equal(0, harness.ReceiptStore.PendingSaveCount);
            Assert.Equal(1, harness.ReceiptStore.UploadedSaveCount);
            Assert.Equal(OperationId, Assert.Single(harness.FileOperator.UploadCalls).UploadOperationId);
            Assert.Equal(OperationId, Assert.Single(harness.ReceiptStore.Receipts).OperationId);
        }

        [Fact]
        public async Task Execute_retry_rejects_missing_matching_pending_receipt_before_upload()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.KeepOriginals);

            await Assert.ThrowsAsync<InvalidDataException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem(OperationId))));

            Assert.Equal(new[] { "receipt:load" }, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
            Assert.Equal(0, harness.ReceiptStore.PendingSaveCount);
            Assert.Equal(0, harness.ReceiptStore.UploadedSaveCount);
        }

        [Fact]
        public async Task Execute_retry_rejects_pending_receipt_for_different_content()
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

            Assert.Equal(new[] { "receipt:load" }, harness.Events);
            Assert.Empty(harness.FileOperator.UploadCalls);
        }

        [Fact]
        public async Task Execute_upload_failure_leaves_pending_receipt_and_does_not_delete()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.FileOperator.UploadException = new IOException("Upload failed.");

            await Assert.ThrowsAsync<IOException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem())));

            Assert.Equal(new[] { "receipt:pending", "remote:upload" }, harness.Events);
            CottonUploadReceiptSnapshot receipt = Assert.Single(harness.ReceiptStore.Receipts);
            Assert.True(receipt.IsPending);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
        }

        [Fact]
        public async Task Execute_uploaded_receipt_failure_prevents_original_delete()
        {
            ExecutionHarness harness = new(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            harness.ReceiptStore.ThrowOnUploadedSave = true;

            await Assert.ThrowsAsync<IOException>(() => harness.Executor.ExecuteAsync(
                InstanceUri,
                harness.Root,
                CreatePlan(CreateUploadItem())));

            Assert.Equal(
                new[] { "receipt:pending", "remote:upload", "receipt:uploaded" },
                harness.Events);
            CottonUploadReceiptSnapshot receipt = Assert.Single(harness.ReceiptStore.Receipts);
            Assert.True(receipt.IsPending);
            Assert.Empty(harness.LocalFileOperator.DeleteCalls);
        }

        [Theory]
        [InlineData(CottonUploadOriginalRetention.KeepOriginals, false)]
        [InlineData(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload, true)]
        public async Task Execute_confirms_pending_without_upload_then_optionally_deletes_original(
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
        public async Task Execute_counts_standalone_cleanup_status(
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

            Assert.Equal(new[] { "local:delete" }, harness.Events);
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
        public async Task Execute_rejects_two_way_mirror_actions(CottonDeviceToCloudSyncActionKind action)
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
        public async Task Execute_rejects_plan_with_different_root_or_folder(bool changesRootId)
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
