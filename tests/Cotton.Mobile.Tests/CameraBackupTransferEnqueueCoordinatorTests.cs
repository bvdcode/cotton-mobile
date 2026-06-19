using System.Text;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public sealed class CameraBackupTransferEnqueueCoordinatorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid DestinationFolderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid TransferId = Guid.Parse("22222222-3333-4444-5555-666666666666");
        private static readonly Guid ExistingTransferId = Guid.Parse("33333333-4444-5555-6666-777777777777");
        private static readonly Guid RemoteFileId = Guid.Parse("44444444-5555-6666-7777-888888888888");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 21, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ModifiedAt = new(2026, 6, 18, 10, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime CapturedAt = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonTransferMetadataStore _transferMetadataStore;
        private readonly FileSystemCottonTransferStagingStore _transferStagingStore;

        public CameraBackupTransferEnqueueCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-camera-backup-transfer-enqueue-tests",
                Guid.NewGuid().ToString("N"));
            _transferMetadataStore = new FileSystemCottonTransferMetadataStore(
                new FixedTransferMetadataPathProvider(Path.Combine(_directory, "transfer")),
                new FixedTimeProvider(CreatedAt));
            _transferStagingStore = new FileSystemCottonTransferStagingStore(
                new FixedTransferStagingPathProvider(Path.Combine(_directory, "transfer", "Staged")));
        }

        [Fact]
        public async Task Enqueue_stages_pending_camera_media_with_source_metadata()
        {
            CottonCameraBackupCandidate candidate = CreatePhotoCandidate();
            var mediaSource = new StubCameraBackupMediaContentSource(
                [candidate],
                new Dictionary<string, string>
                {
                    [candidate.Identity.SourceId] = "camera payload",
                });
            CottonCameraBackupTransferEnqueueCoordinator coordinator =
                CreateCoordinator(mediaSource, [], TransferId);

            CottonCameraBackupTransferEnqueueResult result =
                await coordinator.EnqueueAsync(InstanceUri, CreateSettings());

            Assert.True(result.HasQueuedTransfers);
            Assert.False(result.MissingDestination);
            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(1, result.QueuedCount);
            Assert.Equal(0, result.SkippedExistingTransferCount);
            Assert.Equal(0, result.MissingStreamCount);

            CottonTransferQueueItem transfer = Assert.Single(await _transferMetadataStore.LoadAsync(InstanceUri));
            Assert.Equal(TransferId, transfer.Id);
            Assert.Equal("IMG_0001.jpg", transfer.DisplayName);
            Assert.Equal("image/jpeg", transfer.ContentType);
            Assert.Equal(CottonTransferStatus.Queued, transfer.Status);
            Assert.Equal(14, transfer.Progress.TotalBytes);
            Assert.Equal(DestinationFolderId, transfer.Destination?.FolderId);
            Assert.Equal("Files / Camera", transfer.Destination?.Path);
            Assert.NotNull(transfer.Source);
            Assert.Equal(CottonTransferSourceKind.CameraBackup, transfer.Source.Kind);
            Assert.Equal(candidate.Identity.SourceId, transfer.Source.SourceId);
            Assert.Equal(candidate.Identity.LastModifiedUtc, transfer.Source.LastModifiedUtc);
            Assert.Equal(candidate.Identity.SizeBytes, transfer.Source.SizeBytes);
            Assert.Equal(candidate.CapturedAtUtc, transfer.Source.CapturedAtUtc);

            CottonTransferStagedFileSnapshot staged = Assert.Single(await _transferStagingStore.ListAsync(InstanceUri));
            Assert.Equal(TransferId, staged.TransferId);
            Assert.Equal("IMG_0001.jpg", staged.FileName);
            Assert.Equal("camera payload", await File.ReadAllTextAsync(staged.Path));
        }

        [Fact]
        public async Task Enqueue_requires_a_destination_before_scanning()
        {
            var mediaSource = new StubCameraBackupMediaContentSource(
                [CreatePhotoCandidate()],
                new Dictionary<string, string>());
            CottonCameraBackupTransferEnqueueCoordinator coordinator =
                CreateCoordinator(mediaSource, [], TransferId);

            CottonCameraBackupTransferEnqueueResult result =
                await coordinator.EnqueueAsync(InstanceUri, CottonCameraBackupSettings.Default);

            Assert.True(result.MissingDestination);
            Assert.Equal(0, result.ScannedCount);
            Assert.Equal(0, mediaSource.ListCallCount);
            Assert.Empty(await _transferMetadataStore.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Enqueue_skips_existing_camera_backup_transfer()
        {
            CottonCameraBackupCandidate candidate = CreatePhotoCandidate();
            CottonTransferQueueItem existing = CottonTransferQueueItem.CreateUpload(
                ExistingTransferId,
                candidate.DisplayName,
                candidate.Identity.SizeBytes,
                CreatedAt,
                new CottonTransferDestinationSnapshot(DestinationFolderId, "Camera", "Files / Camera"),
                candidate.ContentType,
                CottonTransferSourceSnapshot.CreateCameraBackup(candidate));
            await _transferMetadataStore.SaveAsync(InstanceUri, [existing]);
            var mediaSource = new StubCameraBackupMediaContentSource(
                [candidate],
                new Dictionary<string, string>
                {
                    [candidate.Identity.SourceId] = "camera payload",
                });
            CottonCameraBackupTransferEnqueueCoordinator coordinator =
                CreateCoordinator(mediaSource, [], TransferId);

            CottonCameraBackupTransferEnqueueResult result =
                await coordinator.EnqueueAsync(InstanceUri, CreateSettings());

            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(0, result.QueuedCount);
            Assert.Equal(1, result.SkippedExistingTransferCount);
            CottonTransferQueueItem remaining = Assert.Single(await _transferMetadataStore.LoadAsync(InstanceUri));
            Assert.Equal(ExistingTransferId, remaining.Id);
            Assert.Empty(await _transferStagingStore.ListAsync(InstanceUri));
        }

        [Fact]
        public async Task Enqueue_counts_missing_media_stream_without_writing_queue_metadata()
        {
            CottonCameraBackupCandidate candidate = CreatePhotoCandidate();
            var mediaSource = new StubCameraBackupMediaContentSource(
                [candidate],
                new Dictionary<string, string>());
            CottonCameraBackupTransferEnqueueCoordinator coordinator =
                CreateCoordinator(mediaSource, [], TransferId);

            CottonCameraBackupTransferEnqueueResult result =
                await coordinator.EnqueueAsync(InstanceUri, CreateSettings());

            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(0, result.QueuedCount);
            Assert.Equal(1, result.MissingStreamCount);
            Assert.Empty(await _transferMetadataStore.LoadAsync(InstanceUri));
            Assert.Empty(await _transferStagingStore.ListAsync(InstanceUri));
        }

        [Fact]
        public async Task Enqueue_uses_planning_service_to_suppress_uploaded_media()
        {
            CottonCameraBackupCandidate candidate = CreatePhotoCandidate();
            var uploaded = new[]
            {
                new CottonCameraBackupUploadedMediaSnapshot(
                    candidate.Identity,
                    CreatedAt.AddMinutes(-5),
                    RemoteFileId,
                    candidate.DisplayName),
            };
            var mediaSource = new StubCameraBackupMediaContentSource(
                [candidate],
                new Dictionary<string, string>
                {
                    [candidate.Identity.SourceId] = "camera payload",
                });
            CottonCameraBackupTransferEnqueueCoordinator coordinator =
                CreateCoordinator(mediaSource, uploaded, TransferId);

            CottonCameraBackupTransferEnqueueResult result =
                await coordinator.EnqueueAsync(InstanceUri, CreateSettings());

            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(0, result.QueuedCount);
            Assert.Equal(0, result.SkippedExistingTransferCount);
            Assert.Empty(await _transferMetadataStore.LoadAsync(InstanceUri));
            Assert.Empty(await _transferStagingStore.ListAsync(InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private CottonCameraBackupTransferEnqueueCoordinator CreateCoordinator(
            StubCameraBackupMediaContentSource mediaSource,
            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> uploaded,
            params Guid[] transferIds)
        {
            var queuedTransferIds = new Queue<Guid>(transferIds);
            var uploadedStore = new StubUploadedMediaStore(uploaded);
            var scanner = new CottonCameraBackupScanner(mediaSource);
            var planningService = new CottonCameraBackupPlanningService(uploadedStore, scanner);
            return new CottonCameraBackupTransferEnqueueCoordinator(
                planningService,
                mediaSource,
                _transferMetadataStore,
                _transferStagingStore,
                new FixedTimeProvider(CreatedAt),
                () => queuedTransferIds.Dequeue());
        }

        private static CottonCameraBackupSettings CreateSettings()
        {
            return CottonCameraBackupSettings.Default.WithDestination(
                new CottonUploadDestinationSnapshot(
                    DestinationFolderId,
                    "Camera",
                    "Files / Camera"));
        }

        private static CottonCameraBackupCandidate CreatePhotoCandidate()
        {
            return new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity(
                    "content://media/external/images/media/100",
                    ModifiedAt,
                    128),
                CottonCameraBackupMediaKind.Photo,
                "IMG_0001.jpg",
                "image/jpeg",
                CapturedAt);
        }

        private sealed class StubCameraBackupMediaContentSource :
            ICottonCameraBackupMediaSource,
            ICottonCameraBackupMediaContentSource
        {
            private readonly IReadOnlyList<CottonCameraBackupCandidate> _candidates;
            private readonly IReadOnlyDictionary<string, string> _contentBySourceId;

            public StubCameraBackupMediaContentSource(
                IReadOnlyList<CottonCameraBackupCandidate> candidates,
                IReadOnlyDictionary<string, string> contentBySourceId)
            {
                _candidates = candidates;
                _contentBySourceId = contentBySourceId;
            }

            public int ListCallCount { get; private set; }

            public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ListCallCount++;
                return Task.FromResult(_candidates);
            }

            public Task<Stream?> OpenReadAsync(
                CottonCameraBackupCandidate candidate,
                CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(candidate);
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(
                    _contentBySourceId.TryGetValue(candidate.Identity.SourceId, out string? content)
                        ? new MemoryStream(Encoding.UTF8.GetBytes(content)) as Stream
                        : null);
            }
        }

        private sealed class StubUploadedMediaStore : ICottonCameraBackupUploadedMediaStore
        {
            private readonly IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> _uploaded;

            public StubUploadedMediaStore(IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> uploaded)
            {
                _uploaded = uploaded;
            }

            public Task<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(instanceUri);
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(_uploaded);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddOrReplaceAsync(
                Uri instanceUri,
                CottonCameraBackupUploadedMediaSnapshot item,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FixedTransferMetadataPathProvider : ICottonTransferMetadataPathProvider
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

        private sealed class FixedTransferStagingPathProvider : ICottonTransferStagingPathProvider
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
