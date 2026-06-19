using System.Text;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShareTransferEnqueueCoordinatorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid IntakeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid ItemId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly Guid TransferId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000");
        private static readonly Guid SecondItemId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");
        private static readonly Guid SecondTransferId = Guid.Parse("eeeeeeee-ffff-0000-1111-222222222222");
        private static readonly Guid DestinationFolderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 18, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonShareIntakeStore _shareIntakeStore;
        private readonly FileSystemCottonShareContentStagingStore _shareStagingStore;
        private readonly FileSystemCottonTransferMetadataStore _transferMetadataStore;
        private readonly FileSystemCottonTransferStagingStore _transferStagingStore;

        public ShareTransferEnqueueCoordinatorTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-share-transfer-enqueue-tests", Guid.NewGuid().ToString("N"));
            var sharePathProvider = new FixedShareIntakePathProvider(Path.Combine(_directory, "share"));
            _shareIntakeStore = new FileSystemCottonShareIntakeStore(sharePathProvider);
            _shareStagingStore = new FileSystemCottonShareContentStagingStore(sharePathProvider);
            _transferMetadataStore = new FileSystemCottonTransferMetadataStore(
                new FixedTransferMetadataPathProvider(Path.Combine(_directory, "transfer")),
                new FixedTimeProvider(CreatedAt));
            _transferStagingStore = new FileSystemCottonTransferStagingStore(
                new FixedTransferStagingPathProvider(Path.Combine(_directory, "transfer", "Staged")));
        }

        [Fact]
        public async Task Enqueue_copies_destination_ready_share_file_into_transfer_queue()
        {
            await SaveDestinationReadyShareAsync("capture.txt", "queued-capture.txt", "hello capture");

            CottonShareTransferEnqueueResult result = await CreateCoordinator().EnqueueAsync(InstanceUri);

            Assert.True(result.HasQueuedTransfers);
            Assert.Equal(1, result.QueuedCount);
            Assert.Equal(0, result.RemainingCaptureCount);

            CottonTransferQueueItem transfer = Assert.Single(await _transferMetadataStore.LoadAsync(InstanceUri));
            Assert.Equal(TransferId, transfer.Id);
            Assert.Equal("queued-capture.txt", transfer.DisplayName);
            Assert.Equal(CottonTransferStatus.Queued, transfer.Status);
            Assert.Equal(13, transfer.Progress.TotalBytes);
            Assert.Equal(DestinationFolderId, transfer.Destination?.FolderId);
            Assert.Equal("Default", transfer.Destination?.Path);

            CottonTransferStagedFileSnapshot stagedTransfer =
                Assert.Single(await _transferStagingStore.ListAsync(InstanceUri));
            Assert.Equal(TransferId, stagedTransfer.TransferId);
            Assert.Equal("queued-capture.txt", stagedTransfer.FileName);
            Assert.Equal("hello capture", await File.ReadAllTextAsync(stagedTransfer.Path));

            Assert.Empty(await _shareIntakeStore.LoadAsync());
            Assert.Empty(await _shareStagingStore.ListAsync());
        }

        [Fact]
        public async Task Enqueue_copies_multiple_destination_ready_share_files_into_transfer_queue()
        {
            await SaveDestinationReadyShareAsync(
                [
                    new ShareFileSeed(ItemId, "first-capture.txt", "first-upload.txt", "first capture"),
                    new ShareFileSeed(SecondItemId, "second-capture.txt", "second-upload.txt", "second capture"),
                ]);

            CottonShareTransferEnqueueResult result = await CreateCoordinator(
                TransferId,
                SecondTransferId).EnqueueAsync(InstanceUri);

            Assert.True(result.HasQueuedTransfers);
            Assert.Equal(2, result.QueuedCount);
            Assert.Equal(0, result.RemainingCaptureCount);
            Assert.Equal([TransferId, SecondTransferId], result.QueuedTransfers.Select(item => item.Id).ToArray());

            IReadOnlyList<CottonTransferQueueItem> transfers =
                await _transferMetadataStore.LoadAsync(InstanceUri);
            Assert.Equal(2, transfers.Count);
            Assert.Contains(
                transfers,
                item => item.Id == TransferId
                    && item.DisplayName == "first-upload.txt"
                    && item.Progress.TotalBytes == 13
                    && item.Destination?.FolderId == DestinationFolderId);
            Assert.Contains(
                transfers,
                item => item.Id == SecondTransferId
                    && item.DisplayName == "second-upload.txt"
                    && item.Progress.TotalBytes == 14
                    && item.Destination?.Path == "Default");

            IReadOnlyList<CottonTransferStagedFileSnapshot> stagedTransfers =
                await _transferStagingStore.ListAsync(InstanceUri);
            Assert.Equal(
                ["first-upload.txt", "second-upload.txt"],
                stagedTransfers.Select(item => item.FileName).OrderBy(item => item, StringComparer.Ordinal).ToArray());
            Assert.Contains(
                stagedTransfers,
                item => item.TransferId == TransferId
                    && File.ReadAllText(item.Path) == "first capture");
            Assert.Contains(
                stagedTransfers,
                item => item.TransferId == SecondTransferId
                    && File.ReadAllText(item.Path) == "second capture");
            Assert.Empty(await _shareIntakeStore.LoadAsync());
            Assert.Empty(await _shareStagingStore.ListAsync());
        }

        [Fact]
        public async Task Enqueue_leaves_text_and_missing_destination_captures_in_inbox()
        {
            CottonShareStagedContentSnapshot staged = await _shareStagingStore.StageAsync(
                IntakeId,
                ItemId,
                "capture.txt",
                new MemoryStream(Encoding.UTF8.GetBytes("hello")));
            var fileItem = new CottonShareIntakeItemSnapshot(
                    ItemId,
                    CottonShareIntakeItemType.Uri,
                    "content://capture",
                    "capture.txt",
                    "text/plain")
                .WithStagedContent(staged);
            var textItem = new CottonShareIntakeItemSnapshot(
                Guid.Parse("dddddddd-eeee-ffff-0000-111111111111"),
                CottonShareIntakeItemType.Text,
                "shared text",
                displayName: null,
                mimeType: "text/plain");
            CottonShareIntakeSnapshot snapshot = CottonShareIntakeSnapshot.CreatePending(
                IntakeId,
                CottonShareIntakeKind.SendMultiple,
                "text/plain",
                [fileItem, textItem],
                CreatedAt);
            await _shareIntakeStore.SaveAsync([snapshot]);

            CottonShareTransferEnqueueResult result = await CreateCoordinator().EnqueueAsync(InstanceUri);

            Assert.False(result.HasQueuedTransfers);
            Assert.Empty(await _transferMetadataStore.LoadAsync(InstanceUri));
            CottonShareIntakeSnapshot remaining = Assert.Single(await _shareIntakeStore.LoadAsync());
            Assert.Equal([fileItem.Id, textItem.Id], remaining.Items.Select(item => item.Id).ToArray());
            Assert.Single(await _shareStagingStore.ListAsync());
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private async Task SaveDestinationReadyShareAsync(
            string stagedName,
            string uploadName,
            string content)
        {
            await SaveDestinationReadyShareAsync(
                [new ShareFileSeed(ItemId, stagedName, uploadName, content)]);
        }

        private async Task SaveDestinationReadyShareAsync(IReadOnlyList<ShareFileSeed> files)
        {
            var items = new List<CottonShareIntakeItemSnapshot>();
            foreach (ShareFileSeed file in files)
            {
                CottonShareStagedContentSnapshot staged = await _shareStagingStore.StageAsync(
                    IntakeId,
                    file.ItemId,
                    file.StagedName,
                    new MemoryStream(Encoding.UTF8.GetBytes(file.Content)));
                CottonShareIntakeItemSnapshot item = new CottonShareIntakeItemSnapshot(
                        file.ItemId,
                        CottonShareIntakeItemType.Uri,
                        $"content://capture/{file.StagedName}",
                        file.StagedName,
                        "text/plain")
                    .WithStagedContent(staged)
                    .WithUploadDisplayName(file.UploadName);
                items.Add(item);
            }

            CottonShareIntakeSnapshot snapshot = CottonShareIntakeSnapshot
                .CreatePending(
                    IntakeId,
                    files.Count == 1 ? CottonShareIntakeKind.Send : CottonShareIntakeKind.SendMultiple,
                    "text/plain",
                    items,
                    CreatedAt)
                .WithDestination(
                    new CottonShareDestinationSnapshot(
                        DestinationFolderId,
                        "Default",
                        "Default"));
            await _shareIntakeStore.SaveAsync([snapshot]);
        }

        private CottonShareTransferEnqueueCoordinator CreateCoordinator()
        {
            return CreateCoordinator(TransferId);
        }

        private CottonShareTransferEnqueueCoordinator CreateCoordinator(params Guid[] transferIds)
        {
            var queuedTransferIds = new Queue<Guid>(transferIds);
            return new CottonShareTransferEnqueueCoordinator(
                _shareIntakeStore,
                _shareStagingStore,
                _transferMetadataStore,
                _transferStagingStore,
                new FixedTimeProvider(CreatedAt),
                () => queuedTransferIds.Dequeue());
        }

        private sealed record ShareFileSeed(Guid ItemId, string StagedName, string UploadName, string Content);

        private class FixedShareIntakePathProvider : ICottonShareIntakePathProvider
        {
            private readonly string _directory;

            public FixedShareIntakePathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateShareIntakeDirectory()
            {
                return _directory;
            }
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
