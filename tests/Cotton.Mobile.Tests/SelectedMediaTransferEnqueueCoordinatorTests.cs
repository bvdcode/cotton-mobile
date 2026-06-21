using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SelectedMediaTransferEnqueueCoordinatorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid DestinationFolderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid FirstTransferId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondTransferId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly DateTime CreatedAt = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime PhotoModifiedAt = new(2026, 6, 20, 10, 15, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Enqueue_stages_selected_media_and_appends_queue_items()
        {
            CottonTransferQueueItem existing = CottonTransferQueueItem.CreateUpload(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "existing.pdf",
                5,
                CreatedAt.AddMinutes(-5));
            var metadataStore = new FakeTransferMetadataStore([existing]);
            var stagingStore = new FakeTransferStagingStore();
            var coordinator = new CottonSelectedMediaTransferEnqueueCoordinator(
                metadataStore,
                stagingStore,
                new FixedTimeProvider(CreatedAt),
                new QueueGuidFactory([FirstTransferId, SecondTransferId]).Next);
            CottonFolderHandle destination = new(DestinationFolderId, "Camera Uploads");
            IReadOnlyList<CottonFileUploadSource> sources =
            [
                CreateSource("photo.jpg", "image/jpeg", "picked-photo", PhotoModifiedAt, "photo-bytes"),
                CreateSource("clip.mp4", "video/mp4", "picked-video", null, "video-bytes"),
            ];

            CottonSelectedMediaTransferEnqueueResult result = await coordinator.EnqueueAsync(
                InstanceUri,
                destination,
                "Files / Camera Uploads",
                sources);

            Assert.Equal(2, result.SelectedCount);
            Assert.Equal(2, result.QueuedCount);
            Assert.Equal("Files / Camera Uploads", result.Destination?.Path);
            Assert.Equal(["photo.jpg", "clip.mp4"], stagingStore.StagedFileNames);
            Assert.Equal(["photo-bytes", "video-bytes"], stagingStore.StagedContent);
            Assert.Equal(3, metadataStore.SavedItems.Count);
            Assert.Same(existing, metadataStore.SavedItems[0]);

            CottonTransferQueueItem photoTransfer = metadataStore.SavedItems[1];
            Assert.Equal(FirstTransferId, photoTransfer.Id);
            Assert.Equal("photo.jpg", photoTransfer.DisplayName);
            Assert.Equal("image/jpeg", photoTransfer.ContentType);
            Assert.Equal(DestinationFolderId, photoTransfer.Destination?.FolderId);
            Assert.Equal("Files / Camera Uploads", photoTransfer.Destination?.Path);
            Assert.Equal(CottonTransferSourceKind.SelectedMedia, photoTransfer.Source?.Kind);
            Assert.Equal(PhotoModifiedAt, photoTransfer.Source?.LastModifiedUtc);
            Assert.Equal(11, photoTransfer.Source?.SizeBytes);
            Assert.Equal(CreatedAt, photoTransfer.Source?.CapturedAtUtc);
            Assert.False(string.IsNullOrWhiteSpace(photoTransfer.Source?.SourceId));
            Assert.DoesNotContain("photo.jpg", photoTransfer.Source!.SourceId, StringComparison.OrdinalIgnoreCase);

            CottonTransferQueueItem videoTransfer = metadataStore.SavedItems[2];
            Assert.Equal(SecondTransferId, videoTransfer.Id);
            Assert.Equal("clip.mp4", videoTransfer.DisplayName);
            Assert.Equal("video/mp4", videoTransfer.ContentType);
            Assert.Null(videoTransfer.Source?.LastModifiedUtc);
        }

        [Fact]
        public async Task Empty_selection_returns_without_saving_or_staging()
        {
            var metadataStore = new FakeTransferMetadataStore([]);
            var stagingStore = new FakeTransferStagingStore();
            var coordinator = new CottonSelectedMediaTransferEnqueueCoordinator(
                metadataStore,
                stagingStore,
                new FixedTimeProvider(CreatedAt));

            CottonSelectedMediaTransferEnqueueResult result = await coordinator.EnqueueAsync(
                InstanceUri,
                new CottonFolderHandle(DestinationFolderId, "Photos"),
                "Photos",
                []);

            Assert.Equal(0, result.SelectedCount);
            Assert.Equal(0, result.QueuedCount);
            Assert.False(metadataStore.WasSaved);
            Assert.Empty(stagingStore.StagedFileNames);
        }

        [Fact]
        public async Task Enqueue_rejects_empty_transfer_ids()
        {
            var coordinator = new CottonSelectedMediaTransferEnqueueCoordinator(
                new FakeTransferMetadataStore([]),
                new FakeTransferStagingStore(),
                new FixedTimeProvider(CreatedAt),
                () => Guid.Empty);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.EnqueueAsync(
                    InstanceUri,
                    new CottonFolderHandle(DestinationFolderId, "Photos"),
                    "Photos",
                    [CreateSource("photo.jpg", "image/jpeg", "picked-photo", null, "photo")]));
        }

        [Fact]
        public void Selected_media_status_text_summarizes_queue_and_schedule()
        {
            var destination = new CottonTransferDestinationSnapshot(DestinationFolderId, "Photos", "Files / Photos");
            CottonTransferQueueItem first = CottonTransferQueueItem.CreateUpload(
                FirstTransferId,
                "photo.jpg",
                11,
                CreatedAt,
                destination,
                "image/jpeg",
                new CottonTransferSourceSnapshot(
                    CottonTransferSourceKind.SelectedMedia,
                    "source",
                    PhotoModifiedAt,
                    11,
                    CreatedAt));
            var one = new CottonSelectedMediaTransferEnqueueResult(1, destination, [first]);
            var three = new CottonSelectedMediaTransferEnqueueResult(3, destination, [first, first, first]);
            CottonAndroidBackgroundTransferRequest request = new(
                InstanceUri,
                FirstTransferId,
                "photo.jpg",
                CottonAndroidTransferExecutionStrategyResolver.Resolve(
                    CottonAndroidTransferWorkKind.SelectedMediaUpload,
                    androidApiLevel: 34),
                estimatedUploadBytes: 11,
                requiresNetwork: true,
                requiresUnmeteredNetwork: false,
                requiresCharging: false);
            CottonAndroidBackgroundTransferScheduleResult scheduled =
                CottonAndroidBackgroundTransferScheduleResult.Scheduled(request, "Scheduled.");
            CottonAndroidBackgroundTransferScheduleResult foreground =
                CottonAndroidBackgroundTransferScheduleResult.ForegroundRequired(request, "Foreground required.");

            Assert.Equal(
                "Queueing 3 photos: photo.jpg...",
                CottonSelectedMediaUploadStatusText.CreateQueueingStatus("photo", 3, " photo.jpg "));
            Assert.Equal(
                "Queued 1 photo: photo.jpg to Files / Photos.",
                CottonSelectedMediaUploadStatusText.CreateResultStatus("photo", one, scheduleResult: null));
            Assert.Equal(
                "Queued 3 photos to Files / Photos. Android will upload in the background.",
                CottonSelectedMediaUploadStatusText.CreateResultStatus("photo", three, scheduled));
            Assert.Equal(
                "Queued 1 video: photo.jpg to Files / Photos. Open Transfers to run waiting uploads.",
                CottonSelectedMediaUploadStatusText.CreateResultStatus("video", one, foreground));
        }

        private static CottonFileUploadSource CreateSource(
            string name,
            string contentType,
            string sourceKind,
            DateTime? lastModifiedUtc,
            string content)
        {
            var metadata = new Dictionary<string, string>
            {
                [CottonFileUploadMetadataKeys.Source] = sourceKind,
                [CottonFileUploadMetadataKeys.TransferPolicy] =
                    CottonSelectedMediaTransferPolicy.CurrentMetadataValue,
            };
            if (lastModifiedUtc is not null)
            {
                metadata[CottonFileUploadMetadataKeys.OriginalLastModifiedUtc] =
                    lastModifiedUtc.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return new CottonFileUploadSource(
                new CottonFileUploadSourceSnapshot(name, contentType, bytes.Length, metadata),
                _ => Task.FromResult<Stream>(new MemoryStream(bytes)));
        }

        private sealed class FakeTransferMetadataStore : ICottonTransferMetadataStore
        {
            private readonly IReadOnlyList<CottonTransferQueueItem> _items;

            public FakeTransferMetadataStore(IReadOnlyList<CottonTransferQueueItem> items)
            {
                _items = items;
            }

            public bool WasSaved { get; private set; }

            public IReadOnlyList<CottonTransferQueueItem> SavedItems { get; private set; } = [];

            public Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> items,
                CancellationToken cancellationToken = default)
            {
                WasSaved = true;
                SavedItems = items.ToList();
                return Task.CompletedTask;
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTransferStagingStore : ICottonTransferStagingStore
        {
            public IReadOnlyList<string> StagedFileNames => _stagedFileNames;

            public IReadOnlyList<string> StagedContent => _stagedContent;

            private readonly List<string> _stagedFileNames = [];

            private readonly List<string> _stagedContent = [];

            public async Task<CottonTransferStagedFileSnapshot> StageAsync(
                Uri instanceUri,
                Guid transferId,
                string fileName,
                Stream content,
                CancellationToken cancellationToken = default)
            {
                using var reader = new StreamReader(content, leaveOpen: true);
                string text = await reader.ReadToEndAsync(cancellationToken);
                _stagedFileNames.Add(fileName);
                _stagedContent.Add(text);
                return new CottonTransferStagedFileSnapshot(
                    transferId,
                    fileName,
                    $"/tmp/{transferId:N}",
                    System.Text.Encoding.UTF8.GetByteCount(text));
            }

            public Task<CottonTransferStagedFileSnapshot?> GetAsync(
                Uri instanceUri,
                Guid transferId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<CottonTransferStagedFileSnapshot?>(null);
            }

            public Task<IReadOnlyList<CottonTransferStagedFileSnapshot>> ListAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CottonTransferStagedFileSnapshot>>([]);
            }

            public Task DeleteAsync(
                Uri instanceUri,
                Guid transferId,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<CottonTransferStagedFileCleanupResult> CleanupAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> queueItems,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CottonTransferStagedFileCleanupResult(0, 0));
            }
        }

        private sealed class QueueGuidFactory
        {
            private readonly Queue<Guid> _ids;

            public QueueGuidFactory(IEnumerable<Guid> ids)
            {
                _ids = new Queue<Guid>(ids);
            }

            public Guid Next()
            {
                return _ids.Dequeue();
            }
        }

        private sealed class FixedTimeProvider : TimeProvider
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
