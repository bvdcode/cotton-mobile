using System.Text.Json;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class UploadReceiptStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid OperationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid RemoteFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly DateTime LocalUpdatedAtUtc = new(2026, 8, 4, 12, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime RecordedAtUtc = new(2026, 8, 4, 12, 31, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonUploadReceiptStore _store;

        public UploadReceiptStoreTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-upload-receipt-store-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonUploadReceiptStore(new ScopedUploadReceiptPathProvider(_directory));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_pending_receipt()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot receipt = CreatePendingReceipt();

            await _store.SaveAsync(InstanceUri, root, receipt);

            CottonUploadReceiptSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, root));
            Assert.Equal(receipt.LocalSourceId, loaded.LocalSourceId);
            Assert.Equal(receipt.RelativePath, loaded.RelativePath);
            Assert.Equal(receipt.LocalUpdatedAtUtc, loaded.LocalUpdatedAtUtc);
            Assert.Equal(receipt.SizeBytes, loaded.SizeBytes);
            Assert.Equal(receipt.ContentType, loaded.ContentType);
            Assert.Equal(receipt.ContentHash, loaded.ContentHash);
            Assert.Equal(receipt.OperationId, loaded.OperationId);
            Assert.Equal(CottonUploadReceiptStatus.Pending, loaded.Status);
            Assert.True(loaded.IsPending);
            Assert.Null(loaded.RemoteFileId);
            Assert.Null(loaded.RemoteETag);
        }

        [Fact]
        public async Task Save_replaces_pending_receipt_with_uploaded_revision()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot pending = CreatePendingReceipt();
            CottonUploadReceiptSnapshot uploaded = pending.MarkUploaded(
                CottonFileBrowserEntry.CreateFile(
                    RemoteFileId,
                    "photo.jpg",
                    RecordedAtUtc,
                    42,
                    "image/jpeg",
                    previewHashEncryptedHex: null,
                    eTag: "etag-uploaded",
                    CreateOperationMetadata(),
                    TestContentHashes.First),
                RecordedAtUtc);

            await _store.SaveAsync(InstanceUri, root, pending);
            await _store.SaveAsync(InstanceUri, root, uploaded);

            CottonUploadReceiptSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, root));
            Assert.True(loaded.IsUploaded);
            Assert.Equal(RemoteFileId, loaded.RemoteFileId);
            Assert.Equal("etag-uploaded", loaded.RemoteETag);
            Assert.Equal(OperationId, loaded.OperationId);
            string receiptDirectory = new ScopedUploadReceiptPathProvider(_directory)
                .CreateUploadReceiptDirectory(InstanceUri, root);
            Assert.Single(Directory.GetFiles(receiptDirectory, "*.json"));
        }

        [Fact]
        public void Mark_uploaded_rejects_a_different_remote_size()
        {
            CottonUploadReceiptSnapshot pending = CreatePendingReceipt();
            CottonFileBrowserEntry wrongSize = CottonFileBrowserEntry.CreateFile(
                RemoteFileId,
                "photo.jpg",
                RecordedAtUtc,
                41,
                "image/jpeg",
                previewHashEncryptedHex: null,
                eTag: "etag-uploaded",
                CreateOperationMetadata(),
                TestContentHashes.First);

            Assert.Throws<ArgumentException>(() => pending.MarkUploaded(wrongSize, RecordedAtUtc));
        }

        [Fact]
        public async Task Save_accepts_confirmation_after_device_clock_moves_backwards()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot pending = CreatePendingReceipt();
            CottonUploadReceiptSnapshot uploaded = pending.MarkUploaded(
                CreateUploadedFile(),
                pending.RecordedAtUtc.AddMinutes(-1));
            await _store.SaveAsync(InstanceUri, root, pending);

            await _store.SaveAsync(InstanceUri, root, uploaded);

            Assert.True(Assert.Single(await _store.LoadAsync(InstanceUri, root)).IsUploaded);
        }

        [Fact]
        public async Task Receipts_are_isolated_by_sync_root()
        {
            CottonSyncRootSnapshot firstRoot = CreateRoot(RootId, "content://tree/camera");
            CottonSyncRootSnapshot secondRoot = CreateRoot(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "content://tree/downloads");

            await _store.SaveAsync(InstanceUri, firstRoot, CreatePendingReceipt());

            Assert.Single(await _store.LoadAsync(InstanceUri, firstRoot));
            Assert.Empty(await _store.LoadAsync(InstanceUri, secondRoot));
        }

        [Fact]
        public async Task Save_rejects_uploaded_receipt_without_pending_history()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot uploaded = CreatePendingReceipt().MarkUploaded(
                CreateUploadedFile(),
                RecordedAtUtc);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _store.SaveAsync(InstanceUri, root, uploaded));

            Assert.Empty(await _store.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Save_rejects_downgrade_after_upload_and_preserves_uploaded_receipt()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot pending = CreatePendingReceipt();
            CottonUploadReceiptSnapshot uploaded = pending.MarkUploaded(
                CreateUploadedFile(),
                RecordedAtUtc.AddSeconds(1));
            await _store.SaveAsync(InstanceUri, root, pending);
            await _store.SaveAsync(InstanceUri, root, uploaded);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _store.SaveAsync(InstanceUri, root, pending));

            CottonUploadReceiptSnapshot preserved = Assert.Single(await _store.LoadAsync(InstanceUri, root));
            Assert.True(preserved.IsUploaded);
            Assert.Equal(RemoteFileId, preserved.RemoteFileId);
        }

        [Fact]
        public async Task Save_rejects_confirmation_with_changed_operation_and_preserves_pending_receipt()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            CottonUploadReceiptSnapshot pending = CreatePendingReceipt();
            CottonUploadReceiptSnapshot changedOperation = new(
                pending.LocalSourceId,
                pending.RelativePath,
                pending.LocalUpdatedAtUtc,
                pending.SizeBytes,
                pending.ContentType,
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                CottonUploadReceiptStatus.Uploaded,
                pending.RecordedAtUtc.AddSeconds(1),
                RemoteFileId,
                "etag-uploaded");
            await _store.SaveAsync(InstanceUri, root, pending);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _store.SaveAsync(InstanceUri, root, changedOperation));

            CottonUploadReceiptSnapshot preserved = Assert.Single(await _store.LoadAsync(InstanceUri, root));
            Assert.True(preserved.IsPending);
            Assert.Equal(OperationId, preserved.OperationId);
        }

        [Fact]
        public async Task Load_fails_closed_and_preserves_corrupt_receipt()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, "content://tree/camera");
            await _store.SaveAsync(InstanceUri, root, CreatePendingReceipt());
            string receiptDirectory = new ScopedUploadReceiptPathProvider(_directory)
                .CreateUploadReceiptDirectory(InstanceUri, root);
            string receiptPath = Assert.Single(Directory.GetFiles(receiptDirectory, "*.json"));
            await File.WriteAllTextAsync(receiptPath, "{ not valid json");

            await Assert.ThrowsAsync<JsonException>(() => _store.LoadAsync(InstanceUri, root));

            Assert.True(File.Exists(receiptPath));
        }

        [Fact]
        public async Task Save_propagates_storage_failure()
        {
            string blockerPath = Path.Combine(_directory, "blocker");
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(blockerPath, "not a directory");
            FileSystemCottonUploadReceiptStore store = new(
                new FixedUploadReceiptPathProvider(Path.Combine(blockerPath, "receipts")));

            await Assert.ThrowsAnyAsync<IOException>(() =>
                store.SaveAsync(
                    InstanceUri,
                    CreateRoot(RootId, "content://tree/camera"),
                    CreatePendingReceipt()));
        }

        [Fact]
        public async Task Clear_removes_only_the_target_root_receipts()
        {
            CottonSyncRootSnapshot firstRoot = CreateRoot(RootId, "content://tree/camera");
            CottonSyncRootSnapshot secondRoot = CreateRoot(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "content://tree/downloads");
            await _store.SaveAsync(InstanceUri, firstRoot, CreatePendingReceipt());
            await _store.SaveAsync(InstanceUri, secondRoot, CreatePendingReceipt());

            await _store.ClearAsync(InstanceUri, firstRoot);

            Assert.Empty(await _store.LoadAsync(InstanceUri, firstRoot));
            Assert.Single(await _store.LoadAsync(InstanceUri, secondRoot));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonUploadReceiptSnapshot CreatePendingReceipt()
        {
            CottonDeviceToCloudSyncPlanItem item = new(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                "photo.jpg",
                "Camera/photo.jpg",
                cloudItemId: null,
                expectedRemoteETag: null,
                LocalUpdatedAtUtc,
                sizeBytes: 42,
                contentType: "image/jpeg",
                localSourceId: "primary:DCIM/Camera/photo.jpg",
                contentHash: TestContentHashes.First);
            return CottonUploadReceiptSnapshot.CreatePending(item, OperationId, RecordedAtUtc);
        }

        private static CottonFileBrowserEntry CreateUploadedFile()
        {
            return CottonFileBrowserEntry.CreateFile(
                RemoteFileId,
                "photo.jpg",
                RecordedAtUtc,
                42,
                "image/jpeg",
                previewHashEncryptedHex: null,
                eTag: "etag-uploaded",
                CreateOperationMetadata(),
                TestContentHashes.First);
        }

        private static IReadOnlyDictionary<string, string> CreateOperationMetadata()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.UploadOperationId] = OperationId.ToString("N"),
            };
        }

        private static CottonSyncRootSnapshot CreateRoot(Guid id, string localRootKey)
        {
            return new CottonSyncRootSnapshot(
                id,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Camera", "Files / Camera"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    localRootKey,
                    "Camera",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);
        }

    }
}
