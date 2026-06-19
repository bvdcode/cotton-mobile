using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TransferMetadataStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid UploadId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime RestartedAt = new(2026, 6, 19, 12, 5, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonTransferMetadataStore _store;

        public TransferMetadataStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-transfer-tests", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonTransferMetadataStore(
                new FixedTransferMetadataPathProvider(_directory),
                new FixedTimeProvider(RestartedAt));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_transfer_metadata_and_restores_running_work()
        {
            CottonTransferQueueItem running = CottonTransferQueueItem.CreateUpload(
                    UploadId,
                    "photo.jpg",
                    200,
                    CreatedAt)
                .Start(CreatedAt.AddSeconds(5))
                .ReportProgress(50, CreatedAt.AddSeconds(10));
            CottonTransferQueueItem failed = CottonTransferQueueItem.CreateUpload(
                    Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                    "offline.pdf",
                    400,
                    CreatedAt)
                .Start(CreatedAt.AddSeconds(15))
                .Fail("Offline", CreatedAt.AddSeconds(20));

            await _store.SaveAsync(InstanceUri, [running, failed]);

            IReadOnlyList<CottonTransferQueueItem> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Equal(2, loaded.Count);
            CottonTransferQueueItem restoredRunning = loaded.Single(item => item.Id == running.Id);
            Assert.Equal(CottonTransferStatus.Queued, restoredRunning.Status);
            Assert.Equal(50, restoredRunning.Progress.TransferredBytes);
            Assert.Equal(200, restoredRunning.Progress.TotalBytes);
            Assert.Equal(1, restoredRunning.AttemptCount);
            Assert.Equal(RestartedAt, restoredRunning.UpdatedAtUtc);

            CottonTransferQueueItem restoredFailed = loaded.Single(item => item.Id == failed.Id);
            Assert.Equal(CottonTransferStatus.Failed, restoredFailed.Status);
            Assert.Equal("Offline", restoredFailed.FailureMessage);
            Assert.True(restoredFailed.CanRetry);
        }

        [Fact]
        public async Task Save_and_load_preserves_transfer_destination()
        {
            var destination = new CottonTransferDestinationSnapshot(
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
                "Camera Uploads",
                "Files / Camera Uploads");
            CottonTransferQueueItem transfer = CottonTransferQueueItem.CreateUpload(
                UploadId,
                "photo.jpg",
                200,
                CreatedAt,
                destination,
                "image/jpeg");

            await _store.SaveAsync(InstanceUri, [transfer]);

            CottonTransferQueueItem loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(destination.FolderId, loaded.Destination?.FolderId);
            Assert.Equal("Camera Uploads", loaded.Destination?.FolderName);
            Assert.Equal("Files / Camera Uploads", loaded.Destination?.Path);
            Assert.Equal("image/jpeg", loaded.ContentType);
        }

        [Fact]
        public async Task Load_defaults_older_transfer_metadata_without_content_type()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                """
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-19T12:00:00Z",
                  "items": [
                    {
                      "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "kind": 0,
                      "displayName": "old-queue.bin",
                      "status": 0,
                      "transferredBytes": 0,
                      "totalBytes": 100,
                      "attemptCount": 0,
                      "createdAtUtc": "2026-06-19T12:00:00Z",
                      "updatedAtUtc": "2026-06-19T12:00:00Z"
                    }
                  ]
                }
                """);

            CottonTransferQueueItem loaded = Assert.Single(await _store.LoadAsync(InstanceUri));

            Assert.Equal("old-queue.bin", loaded.DisplayName);
            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultContentType, loaded.ContentType);
        }

        [Fact]
        public async Task Load_returns_empty_list_when_metadata_file_is_missing()
        {
            IReadOnlyList<CottonTransferQueueItem> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_file_and_returns_empty_list()
        {
            Directory.CreateDirectory(_directory);
            string metadataPath = CreateMetadataPath();
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonTransferQueueItem> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_transfers()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                """
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-19T12:00:00Z",
                  "items": [
                    {
                      "id": "00000000-0000-0000-0000-000000000000",
                      "kind": 0,
                      "displayName": "bad.jpg",
                      "status": 0,
                      "transferredBytes": 0,
                      "totalBytes": 100,
                      "attemptCount": 0,
                      "createdAtUtc": "2026-06-19T12:00:00Z",
                      "updatedAtUtc": "2026-06-19T12:00:00Z"
                    },
                    {
                      "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "kind": 0,
                      "displayName": "good.jpg",
                      "status": 1,
                      "transferredBytes": 10,
                      "totalBytes": 100,
                      "attemptCount": 1,
                      "createdAtUtc": "2026-06-19T12:00:00Z",
                      "updatedAtUtc": "2026-06-19T12:01:00Z"
                    }
                  ]
                }
                """);

            IReadOnlyList<CottonTransferQueueItem> loaded = await _store.LoadAsync(InstanceUri);

            CottonTransferQueueItem item = Assert.Single(loaded);
            Assert.Equal(UploadId, item.Id);
            Assert.Equal("good.jpg", item.DisplayName);
            Assert.Equal(CottonTransferStatus.Queued, item.Status);
            Assert.Equal(10, item.Progress.TransferredBytes);
        }

        [Fact]
        public async Task Clear_removes_saved_metadata_file()
        {
            CottonTransferQueueItem transfer = CottonTransferQueueItem.CreateUpload(
                UploadId,
                "photo.jpg",
                200,
                CreatedAt);
            await _store.SaveAsync(InstanceUri, [transfer]);

            await _store.ClearAsync(InstanceUri);

            Assert.False(File.Exists(CreateMetadataPath()));
            Assert.Empty(await _store.LoadAsync(InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private string CreateMetadataPath()
        {
            return Path.Combine(_directory, "queue.json");
        }

        private class FixedTransferMetadataPathProvider : ICottonTransferMetadataPathProvider
        {
            private readonly string _directory;

            public FixedTransferMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateTransferMetadataDirectory(Uri instanceUri)
            {
                return _directory;
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
