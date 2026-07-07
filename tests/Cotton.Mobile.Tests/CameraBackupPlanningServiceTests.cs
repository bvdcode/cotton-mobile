using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CameraBackupPlanningServiceTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly DateTime ModifiedAt = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime UploadedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;

        public CameraBackupPlanningServiceTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-camera-backup-planning-tests",
                Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public async Task Planning_empty_source_reports_no_pending_or_uploaded_media()
        {
            var store = new InMemoryUploadedMediaStore();
            var planner = CreatePlanner(store);

            CottonCameraBackupPlanSnapshot plan =
                await planner.PlanAsync(InstanceUri, CottonCameraBackupSettings.Default);

            Assert.Empty(plan.ScanResult.Candidates);
            Assert.Empty(plan.UploadedMedia);
            Assert.Equal(0, plan.Health.PendingCount);
            Assert.Equal(0, plan.Health.UploadedCount);
            Assert.Equal(0, plan.Health.FailedCount);
            Assert.Equal(0, plan.Health.BlockedCount);
        }

        [Fact]
        public async Task Planning_suppresses_already_uploaded_media_and_reports_uploaded_count()
        {
            CottonCameraBackupCandidate uploadedCandidate =
                CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo);
            CottonCameraBackupCandidate pendingCandidate =
                CreateCandidate("media://photo/2", CottonCameraBackupMediaKind.Photo);
            var store = new InMemoryUploadedMediaStore(
                CreateUploadedMedia(uploadedCandidate.Identity, "photo-1.jpg"));
            var planner = CreatePlanner(store, uploadedCandidate, pendingCandidate);

            CottonCameraBackupPlanSnapshot plan =
                await planner.PlanAsync(InstanceUri, CottonCameraBackupSettings.Default);

            CottonCameraBackupCandidate pending = Assert.Single(plan.ScanResult.Candidates);
            Assert.Equal("media://photo/2", pending.Identity.SourceId);
            Assert.Equal(1, plan.ScanResult.SkippedAlreadyTrackedCount);
            Assert.Equal(1, plan.Health.PendingCount);
            Assert.Equal(1, plan.Health.UploadedCount);
        }

        [Fact]
        public async Task Planning_uses_photos_only_setting_for_pending_count()
        {
            var store = new InMemoryUploadedMediaStore();
            var planner = CreatePlanner(
                store,
                CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo),
                CreateCandidate("media://video/1", CottonCameraBackupMediaKind.Video));

            CottonCameraBackupPlanSnapshot photosOnly =
                await planner.PlanAsync(InstanceUri, CottonCameraBackupSettings.Default);
            CottonCameraBackupPlanSnapshot photosAndVideos =
                await planner.PlanAsync(InstanceUri, CottonCameraBackupSettings.Default.WithPhotosOnly(false));

            Assert.Equal(1, photosOnly.Health.PendingCount);
            Assert.Equal(1, photosOnly.ScanResult.SkippedByPolicyCount);
            Assert.Equal(1024, photosOnly.DestinationStorageEstimate.KnownSizeBytes);
            Assert.Equal(1, photosOnly.DestinationStorageEstimate.PendingCount);

            Assert.Equal(2, photosAndVideos.Health.PendingCount);
            Assert.Equal(0, photosAndVideos.ScanResult.SkippedByPolicyCount);
            Assert.Equal(2048, photosAndVideos.DestinationStorageEstimate.KnownSizeBytes);
            Assert.Equal(2, photosAndVideos.DestinationStorageEstimate.PendingCount);
        }

        [Fact]
        public async Task Planning_treats_corrupt_uploaded_store_as_safe_empty_state()
        {
            var pathProvider = new FixedCameraBackupMetadataPathProvider(_directory);
            var store = new FileSystemCottonCameraBackupUploadedMediaStore(pathProvider);
            Directory.CreateDirectory(_directory);
            string metadataPath = Path.Combine(_directory, "uploaded-media.json");
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");
            var planner = new CottonCameraBackupPlanningService(
                store,
                new CottonCameraBackupScanner(
                    new StubCameraBackupMediaSource(
                        CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo))));

            CottonCameraBackupPlanSnapshot plan =
                await planner.PlanAsync(InstanceUri, CottonCameraBackupSettings.Default);

            Assert.Equal(1, plan.Health.PendingCount);
            Assert.Equal(0, plan.Health.UploadedCount);
            Assert.False(File.Exists(metadataPath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonCameraBackupPlanningService CreatePlanner(
            ICottonCameraBackupUploadedMediaStore store,
            params CottonCameraBackupCandidate[] candidates)
        {
            return new CottonCameraBackupPlanningService(
                store,
                new CottonCameraBackupScanner(new StubCameraBackupMediaSource(candidates)));
        }

        private static CottonCameraBackupCandidate CreateCandidate(
            string sourceId,
            CottonCameraBackupMediaKind kind)
        {
            string displayName = kind == CottonCameraBackupMediaKind.Photo ? "photo.jpg" : "video.mp4";
            string contentType = kind == CottonCameraBackupMediaKind.Photo ? "image/jpeg" : "video/mp4";
            return new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity(sourceId, ModifiedAt, 1024),
                kind,
                displayName,
                contentType,
                ModifiedAt);
        }

        private static CottonCameraBackupUploadedMediaSnapshot CreateUploadedMedia(
            CottonCameraBackupMediaIdentity identity,
            string remoteFileName)
        {
            return new CottonCameraBackupUploadedMediaSnapshot(
                identity,
                UploadedAt,
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                remoteFileName);
        }

        private class StubCameraBackupMediaSource : ICottonCameraBackupMediaSource
        {
            private readonly IReadOnlyList<CottonCameraBackupCandidate> _candidates;

            public StubCameraBackupMediaSource(params CottonCameraBackupCandidate[] candidates)
            {
                _candidates = candidates;
            }

            public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(_candidates);
            }
        }

        private class InMemoryUploadedMediaStore : ICottonCameraBackupUploadedMediaStore
        {
            private readonly List<CottonCameraBackupUploadedMediaSnapshot> _items;

            public InMemoryUploadedMediaStore(params CottonCameraBackupUploadedMediaSnapshot[] items)
            {
                _items = items.ToList();
            }

            public Task<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>>(_items.ToArray());
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _items.Clear();
                _items.AddRange(items);
                return Task.CompletedTask;
            }

            public Task AddOrReplaceAsync(
                Uri instanceUri,
                CottonCameraBackupUploadedMediaSnapshot item,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _items.RemoveAll(existing => existing.Identity.Equals(item.Identity));
                _items.Add(item);
                return Task.CompletedTask;
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _items.Clear();
                return Task.CompletedTask;
            }
        }

        private class FixedCameraBackupMetadataPathProvider : ICottonCameraBackupMetadataPathProvider
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
    }
}
