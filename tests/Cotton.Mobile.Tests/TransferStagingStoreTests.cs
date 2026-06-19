using System.Text;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TransferStagingStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid UploadId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 14, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonTransferStagingStore _store;

        public TransferStagingStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-transfer-staging-tests", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonTransferStagingStore(new FixedTransferStagingPathProvider(_directory));
        }

        [Fact]
        public async Task Stage_copies_stream_to_transfer_directory_with_safe_leaf_file_name()
        {
            await using var content = CreateStream("hello");

            CottonTransferStagedFileSnapshot staged = await _store.StageAsync(
                InstanceUri,
                UploadId,
                " /tmp/photos/report.pdf ",
                content);

            Assert.Equal(UploadId, staged.TransferId);
            Assert.Equal("report.pdf", staged.FileName);
            Assert.Equal(5, staged.SizeBytes);
            Assert.Equal("hello", await File.ReadAllTextAsync(staged.Path));
            Assert.Contains(UploadId.ToString("N"), staged.Path, StringComparison.Ordinal);
            Assert.False(Directory.Exists(Path.Combine(_directory, ".temp")));
        }

        [Fact]
        public async Task Stage_replaces_previous_file_for_same_transfer()
        {
            CottonTransferStagedFileSnapshot first = await _store.StageAsync(
                InstanceUri,
                UploadId,
                "first.txt",
                CreateStream("old"));
            CottonTransferStagedFileSnapshot second = await _store.StageAsync(
                InstanceUri,
                UploadId,
                "second.txt",
                CreateStream("new content"));

            CottonTransferStagedFileSnapshot? loaded = await _store.GetAsync(InstanceUri, UploadId);

            Assert.False(File.Exists(first.Path));
            Assert.Equal(second.Path, loaded?.Path);
            Assert.Equal("second.txt", loaded?.FileName);
            Assert.Equal("new content", await File.ReadAllTextAsync(second.Path));
            Assert.Single(Directory.EnumerateFiles(Path.GetDirectoryName(second.Path)!));
        }

        [Fact]
        public async Task Get_returns_null_when_staged_file_is_missing()
        {
            CottonTransferStagedFileSnapshot staged = await _store.StageAsync(
                InstanceUri,
                UploadId,
                "photo.jpg",
                CreateStream("image"));
            File.Delete(staged.Path);

            CottonTransferStagedFileSnapshot? loaded = await _store.GetAsync(InstanceUri, UploadId);

            Assert.Null(loaded);
        }

        [Fact]
        public async Task List_ignores_non_transfer_directories()
        {
            await _store.StageAsync(InstanceUri, UploadId, "photo.jpg", CreateStream("image"));
            Directory.CreateDirectory(Path.Combine(_directory, "not-a-guid"));

            IReadOnlyList<CottonTransferStagedFileSnapshot> stagedFiles = await _store.ListAsync(InstanceUri);

            CottonTransferStagedFileSnapshot staged = Assert.Single(stagedFiles);
            Assert.Equal(UploadId, staged.TransferId);
            Assert.Equal("photo.jpg", staged.FileName);
        }

        [Fact]
        public async Task Cleanup_deletes_terminal_and_orphaned_staged_files_but_keeps_retryable_work()
        {
            Guid completedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid cancelledId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Guid failedId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Guid queuedId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            Guid orphanedId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

            await _store.StageAsync(InstanceUri, completedId, "completed.txt", CreateStream("1"));
            await _store.StageAsync(InstanceUri, cancelledId, "cancelled.txt", CreateStream("2"));
            await _store.StageAsync(InstanceUri, failedId, "failed.txt", CreateStream("3"));
            await _store.StageAsync(InstanceUri, queuedId, "queued.txt", CreateStream("4"));
            await _store.StageAsync(InstanceUri, orphanedId, "orphaned.txt", CreateStream("5"));

            IReadOnlyList<CottonTransferQueueItem> queueItems =
            [
                CreateUpload(completedId).Start(CreatedAt.AddSeconds(1)).Complete(CreatedAt.AddSeconds(2)),
                CreateUpload(cancelledId).Cancel(CreatedAt.AddSeconds(3)),
                CreateUpload(failedId).Start(CreatedAt.AddSeconds(4)).Fail("Offline", CreatedAt.AddSeconds(5)),
                CreateUpload(queuedId),
            ];

            await _store.CleanupAsync(InstanceUri, queueItems);

            IReadOnlyList<CottonTransferStagedFileSnapshot> stagedFiles = await _store.ListAsync(InstanceUri);
            Assert.Equal([failedId, queuedId], stagedFiles.Select(file => file.TransferId).Order().ToArray());
        }

        [Fact]
        public void Cleanup_policy_marks_completed_cancelled_and_orphaned_ids_for_deletion()
        {
            Guid runningId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid completedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Guid cancelledId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Guid orphanedId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

            IReadOnlySet<Guid> deleteIds = CottonTransferStagedFileCleanupPolicy.ResolveTransferIdsToDelete(
                [
                    CreateUpload(runningId).Start(CreatedAt.AddSeconds(1)),
                    CreateUpload(completedId).Start(CreatedAt.AddSeconds(2)).Complete(CreatedAt.AddSeconds(3)),
                    CreateUpload(cancelledId).Cancel(CreatedAt.AddSeconds(4)),
                ],
                [runningId, completedId, cancelledId, orphanedId]);

            Assert.Equal(
                new[] { cancelledId, completedId, orphanedId }.Order().ToArray(),
                deleteIds.Order().ToArray());
        }

        [Fact]
        public async Task Stage_rejects_empty_transfer_id()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.StageAsync(InstanceUri, Guid.Empty, "photo.jpg", CreateStream("image")));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static MemoryStream CreateStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private static CottonTransferQueueItem CreateUpload(Guid id)
        {
            return CottonTransferQueueItem.CreateUpload(id, "upload.bin", 100, CreatedAt);
        }

        private class FixedTransferStagingPathProvider : ICottonTransferStagingPathProvider
        {
            private readonly string _directory;

            public FixedTransferStagingPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateTransferStagingDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }
    }
}
