using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.UploadOnlySyncPlanExecutorTestData;

namespace Cotton.Mobile.Tests
{
    public class UploadReceiptRecoveryTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonUploadReceiptStore _store;

        public UploadReceiptRecoveryTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-upload-receipt-recovery-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonUploadReceiptStore(
                new ScopedUploadReceiptPathProvider(_directory));
        }

        [Fact]
        public async Task ClearPendingPreservesConfirmedUploadHistory()
        {
            CottonSyncRootSnapshot root = CreateRoot(CottonUploadOriginalRetention.KeepOriginals);
            CottonUploadReceiptSnapshot pending = CottonUploadReceiptSnapshot.CreatePending(
                CreateUploadItem(OperationId),
                OperationId,
                RecordedAt);
            CottonUploadReceiptSnapshot confirmedPending = CottonUploadReceiptSnapshot.CreatePending(
                CreateSecondUploadItem(),
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                RecordedAt);
            CottonUploadReceiptSnapshot uploaded = confirmedPending.MarkUploaded(
                CreateSecondUploadedFile(),
                RecordedAt.AddSeconds(1));
            await _store.SaveAsync(InstanceUri, root, pending);
            await _store.SaveAsync(InstanceUri, root, confirmedPending);
            await _store.SaveAsync(InstanceUri, root, uploaded);

            int removedCount = await _store.ClearPendingAsync(InstanceUri, root);

            CottonUploadReceiptSnapshot remaining = Assert.Single(
                await _store.LoadAsync(InstanceUri, root));
            Assert.Equal(1, removedCount);
            Assert.True(remaining.IsUploaded);
            Assert.Equal(uploaded.LocalSourceId, remaining.LocalSourceId);
        }

        public void Dispose()
        {
            _store.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateSecondUploadItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                "second.jpg",
                "second.jpg",
                cloudItemId: null,
                expectedRemoteETag: null,
                LocalUpdatedAt,
                sizeBytes: 84,
                contentType: "image/jpeg",
                localSourceId: "primary:DCIM/Camera/second.jpg",
                contentHash: TestContentHashes.Second);
        }

        private static CottonFileBrowserEntry CreateSecondUploadedFile()
        {
            IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.UploadOperationId] =
                    "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
            };
            return CottonFileBrowserEntryFactory.CreateFile(
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                "second.jpg",
                RecordedAt,
                84,
                "image/jpeg",
                previewHashEncryptedHex: null,
                eTag: "etag-second",
                metadata,
                TestContentHashes.Second);
        }
    }
}
