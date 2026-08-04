// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class UploadOnlySyncPlanExecutorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid RootFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid RemoteFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid OperationId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly DateTime LocalUpdatedAt =
            new(2026, 8, 4, 12, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime RecordedAt =
            new(2026, 8, 4, 12, 31, 0, DateTimeKind.Utc);

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
                new[] { CottonUploadReceiptStatus.Pending, CottonUploadReceiptStatus.Uploaded },
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
                "primary:DCIM/Camera/photo.jpg");

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

        private static CottonDeviceToCloudSyncPlanSnapshot CreatePlan(
            params CottonDeviceToCloudSyncPlanItem[] items)
        {
            return new CottonDeviceToCloudSyncPlanSnapshot(
                SyncRootId,
                RootFolderId,
                "Camera",
                items);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateUploadItem(Guid? operationId = null)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                cloudItemId: null,
                expectedRemoteETag: null,
                LocalUpdatedAt,
                sizeBytes: 42,
                contentType: "image/jpeg",
                localSourceId: "primary:DCIM/Camera/photo.jpg",
                uploadOperationId: operationId);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateConfirmationItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                RemoteFileId,
                "etag-remote",
                LocalUpdatedAt,
                42,
                "image/jpeg",
                "primary:DCIM/Camera/photo.jpg",
                OperationId);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateCleanupItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "photo.jpg",
                RemoteFileId,
                "etag-remote",
                LocalUpdatedAt,
                42,
                "image/jpeg",
                "primary:DCIM/Camera/photo.jpg",
                OperationId);
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonUploadOriginalRetention retention)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(RootFolderId, "Camera", "Files / Camera"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/camera",
                    "Camera",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud,
                retention);
        }

        private class ExecutionHarness
        {
            public ExecutionHarness(
                CottonUploadOriginalRetention retention,
                IReadOnlyList<CottonUploadReceiptSnapshot>? initialReceipts = null)
            {
                Events = [];
                Root = CreateRoot(retention);
                ReceiptStore = new FakeUploadReceiptStore(Events, initialReceipts ?? []);
                FileOperator = new FakeSyncFileOperator(Events);
                LocalFileOperator = new FakeLocalFileOperator(Events);
                Executor = new CottonUploadOnlySyncPlanExecutor(
                    FileOperator,
                    LocalFileOperator,
                    ReceiptStore,
                    new FixedTimeProvider(RecordedAt));
            }

            public List<string> Events { get; }

            public CottonSyncRootSnapshot Root { get; }

            public FakeUploadReceiptStore ReceiptStore { get; }

            public FakeSyncFileOperator FileOperator { get; }

            public FakeLocalFileOperator LocalFileOperator { get; }

            public CottonUploadOnlySyncPlanExecutor Executor { get; }
        }

        private class FakeUploadReceiptStore : ICottonUploadReceiptStore
        {
            private readonly List<string> _events;

            public FakeUploadReceiptStore(
                List<string> events,
                IReadOnlyList<CottonUploadReceiptSnapshot> initialReceipts)
            {
                _events = events;
                Receipts = [.. initialReceipts];
            }

            public List<CottonUploadReceiptSnapshot> Receipts { get; }

            public List<CottonUploadReceiptSnapshot> SaveHistory { get; } = [];

            public int PendingSaveCount { get; private set; }

            public int UploadedSaveCount { get; private set; }

            public bool ThrowOnUploadedSave { get; set; }

            public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                _events.Add("receipt:load");
                IReadOnlyList<CottonUploadReceiptSnapshot> receipts = [.. Receipts];
                return Task.FromResult(receipts);
            }

            public Task SaveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonUploadReceiptSnapshot receipt,
                CancellationToken cancellationToken = default)
            {
                SaveHistory.Add(receipt);
                switch (receipt.Status)
                {
                    case CottonUploadReceiptStatus.Pending:
                        PendingSaveCount++;
                        _events.Add("receipt:pending");
                        break;

                    case CottonUploadReceiptStatus.Uploaded:
                        UploadedSaveCount++;
                        _events.Add("receipt:uploaded");
                        if (ThrowOnUploadedSave)
                        {
                            return Task.FromException(new IOException("Uploaded receipt write failed."));
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(receipt), "Receipt status is not supported.");
                }

                int existingIndex = Receipts.FindIndex(existing =>
                    string.Equals(existing.LocalSourceId, receipt.LocalSourceId, StringComparison.Ordinal));
                if (existingIndex >= 0)
                {
                    Receipts[existingIndex] = receipt;
                }
                else
                {
                    Receipts.Add(receipt);
                }

                return Task.CompletedTask;
            }

            public Task ClearAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                Receipts.Clear();
                return Task.CompletedTask;
            }
        }

        private class FakeSyncFileOperator : ICottonDeviceToCloudSyncFileOperator
        {
            private readonly List<string> _events;

            public FakeSyncFileOperator(List<string> events)
            {
                _events = events;
            }

            public List<CottonDeviceToCloudSyncPlanItem> UploadCalls { get; } = [];

            public Exception? UploadException { get; set; }

            public Task<CottonFileBrowserEntry> UploadNewFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                UploadCalls.Add(item);
                _events.Add("remote:upload");
                if (UploadException is not null)
                {
                    return Task.FromException<CottonFileBrowserEntry>(UploadException);
                }

                Guid operationId = item.UploadOperationId
                    ?? throw new InvalidOperationException("Upload call requires an operation id.");
                IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.ToString("N"),
                };
                CottonFileBrowserEntry uploaded = CottonFileBrowserEntry.CreateFile(
                    RemoteFileId,
                    item.DisplayName,
                    RecordedAt,
                    item.SizeBytes,
                    item.ContentType,
                    previewHashEncryptedHex: null,
                    eTag: "etag-remote",
                    metadata);
                return Task.FromResult(uploaded);
            }

            public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Changed upload was not expected.");
            }

            public Task<CottonFileBrowserEntry> CreateFolderAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Folder creation was not expected.");
            }

            public Task DeleteRemoteFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Remote delete was not expected.");
            }
        }

        private class FakeLocalFileOperator : ICottonDeviceToCloudLocalFileOperator
        {
            private readonly List<string> _events;

            public FakeLocalFileOperator(List<string> events)
            {
                _events = events;
            }

            public CottonDeviceToCloudLocalFileDeleteStatus DeleteStatus { get; set; } =
                CottonDeviceToCloudLocalFileDeleteStatus.Deleted;

            public List<CottonDeviceToCloudSyncPlanItem> DeleteCalls { get; } = [];

            public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                DeleteCalls.Add(item);
                _events.Add("local:delete");
                return Task.FromResult(DeleteStatus);
            }
        }

        private class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
