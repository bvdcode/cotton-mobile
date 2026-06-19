using System.Text;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public sealed class CameraBackupWorkflowTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid DestinationFolderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid TransferId = Guid.Parse("22222222-3333-4444-5555-666666666666");
        private static readonly Guid RemoteFileId = Guid.Parse("33333333-4444-5555-6666-777777777777");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 22, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ExecutedAt = new(2026, 6, 19, 22, 5, 0, DateTimeKind.Utc);
        private static readonly DateTime ModifiedAt = new(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CapturedAt = new(2026, 6, 18, 7, 30, 0, DateTimeKind.Utc);

        private readonly string _directory;

        public CameraBackupWorkflowTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-camera-backup-workflow-tests",
                Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public async Task Completed_queued_camera_backup_upload_suppresses_future_scan_planning()
        {
            CottonCameraBackupCandidate candidate = CreateCandidate();
            var mediaSource = new StubCameraBackupMediaSource(
                [candidate],
                new Dictionary<string, string>
                {
                    [candidate.Identity.SourceId] = "camera payload",
                });
            var uploadedStore = new FileSystemCottonCameraBackupUploadedMediaStore(
                new FixedCameraBackupMetadataPathProvider(Path.Combine(_directory, "camera")));
            var transferMetadataStore = new FileSystemCottonTransferMetadataStore(
                new FixedTransferMetadataPathProvider(Path.Combine(_directory, "transfer")));
            var transferStagingStore = new FileSystemCottonTransferStagingStore(
                new FixedTransferStagingPathProvider(Path.Combine(_directory, "transfer", "Staged")));
            var scanner = new CottonCameraBackupScanner(mediaSource);
            var planningService = new CottonCameraBackupPlanningService(uploadedStore, scanner);
            var enqueueCoordinator = new CottonCameraBackupTransferEnqueueCoordinator(
                planningService,
                mediaSource,
                transferMetadataStore,
                transferStagingStore,
                new FixedTimeProvider(CreatedAt),
                () => TransferId);
            CottonCameraBackupSettings settings = CottonCameraBackupSettings.Default.WithDestination(
                new CottonUploadDestinationSnapshot(
                    DestinationFolderId,
                    "Camera",
                    "Files / Camera"));

            CottonCameraBackupPlanSnapshot beforePlan =
                await planningService.PlanAsync(InstanceUri, settings);
            CottonCameraBackupTransferEnqueueResult enqueueResult =
                await enqueueCoordinator.EnqueueAsync(InstanceUri, settings);
            var executor = new CottonQueuedUploadExecutor(
                transferMetadataStore,
                transferStagingStore,
                new FakeQueuedUploadClient(),
                uploadedStore,
                new FixedTimeProvider(ExecutedAt));
            CottonQueuedUploadExecutionResult executionResult =
                await executor.ExecuteNextAsync(InstanceUri);
            CottonCameraBackupPlanSnapshot afterPlan =
                await planningService.PlanAsync(InstanceUri, settings);

            Assert.Equal(1, beforePlan.Health.PendingCount);
            Assert.Equal(1, enqueueResult.QueuedCount);
            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, executionResult.Status);
            Assert.Equal(0, afterPlan.Health.PendingCount);
            Assert.Equal(1, afterPlan.Health.UploadedCount);

            CottonCameraBackupUploadedMediaSnapshot uploaded =
                Assert.Single(await uploadedStore.LoadAsync(InstanceUri));
            Assert.Equal(candidate.Identity, uploaded.Identity);
            Assert.Equal(RemoteFileId, uploaded.RemoteFileId);
            Assert.Equal("remote-camera.jpg", uploaded.RemoteFileName);
            Assert.Empty(await transferStagingStore.ListAsync(InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonCameraBackupCandidate CreateCandidate()
        {
            return new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity(
                    "content://media/external/images/media/200",
                    ModifiedAt,
                    14),
                CottonCameraBackupMediaKind.Photo,
                "camera.jpg",
                "image/jpeg",
                CapturedAt);
        }

        private sealed class StubCameraBackupMediaSource :
            ICottonCameraBackupMediaSource,
            ICottonCameraBackupMediaContentSource
        {
            private readonly IReadOnlyList<CottonCameraBackupCandidate> _candidates;
            private readonly IReadOnlyDictionary<string, string> _contentBySourceId;

            public StubCameraBackupMediaSource(
                IReadOnlyList<CottonCameraBackupCandidate> candidates,
                IReadOnlyDictionary<string, string> contentBySourceId)
            {
                _candidates = candidates;
                _contentBySourceId = contentBySourceId;
            }

            public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(_candidates);
            }

            public Task<Stream?> OpenReadAsync(
                CottonCameraBackupCandidate candidate,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(
                    _contentBySourceId.TryGetValue(candidate.Identity.SourceId, out string? content)
                        ? new MemoryStream(Encoding.UTF8.GetBytes(content)) as Stream
                        : null);
            }
        }

        private sealed class FakeQueuedUploadClient : ICottonQueuedUploadClient
        {
            public async Task<CottonQueuedUploadClientResult> UploadAsync(
                Uri instanceUri,
                CottonTransferQueueItem transfer,
                CottonTransferStagedFileSnapshot stagedFile,
                Func<long, CancellationToken, Task> reportProgressAsync,
                CancellationToken cancellationToken = default)
            {
                await reportProgressAsync(stagedFile.SizeBytes, cancellationToken);
                return new CottonQueuedUploadClientResult(RemoteFileId, "remote-camera.jpg");
            }
        }

        private sealed class FixedCameraBackupMetadataPathProvider : ICottonCameraBackupMetadataPathProvider
        {
            private readonly string _directory;

            public FixedCameraBackupMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateCameraBackupMetadataDirectory(Uri instanceUri)
            {
                return _directory;
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
