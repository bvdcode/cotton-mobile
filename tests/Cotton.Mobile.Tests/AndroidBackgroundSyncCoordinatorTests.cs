using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidBackgroundSyncCoordinatorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFolderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public async Task Schedules_ready_unpaused_cloud_to_device_roots()
        {
            CottonSyncRootSnapshot first = CreateRoot(RootId, FolderId, "Projects");
            CottonSyncRootSnapshot second = CreateRoot(SecondRootId, SecondFolderId, "Archive");
            var host = new FakeBackgroundSyncHost();
            var coordinator = new CottonAndroidBackgroundSyncCoordinator(
                new FakeSyncRootStore([first, second]),
                new FakeSyncRootPauseStore(new HashSet<Guid>()),
                host);

            CottonAndroidBackgroundSyncScheduleResult result = await coordinator.ScheduleAsync(InstanceUri);

            Assert.True(result.IsScheduled);
            CottonAndroidBackgroundSyncRequest request = Assert.Single(host.Requests);
            Assert.Equal(2, request.EligibleRootCount);
            Assert.Equal("cotton-sync-d20f82574bbbb5a5", request.ScheduleIdentity.UniqueWorkName);
            Assert.Equal(request.ScheduleIdentity.UniqueWorkName, request.ScheduleIdentity.SyncTag);
            Assert.DoesNotContain("app.cottoncloud.dev", request.ScheduleIdentity.UniqueWorkName);
            Assert.True(request.RequiresNetwork);
            Assert.Same(request, result.Request);
        }

        [Fact]
        public async Task Skips_paused_not_ready_and_unsupported_direction_roots_without_calling_host()
        {
            CottonSyncRootSnapshot paused = CreateRoot(RootId, FolderId, "Projects");
            CottonSyncRootSnapshot notReady = CreateRoot(
                SecondRootId,
                SecondFolderId,
                "Archive",
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.CloudToDevice);
            CottonSyncRootSnapshot deviceToCloud = CreateRoot(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Upload",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud);
            var host = new FakeBackgroundSyncHost();
            var coordinator = new CottonAndroidBackgroundSyncCoordinator(
                new FakeSyncRootStore([paused, notReady, deviceToCloud]),
                new FakeSyncRootPauseStore(new HashSet<Guid> { paused.Id }),
                host);

            CottonAndroidBackgroundSyncScheduleResult result = await coordinator.ScheduleAsync(InstanceUri);

            Assert.Equal(CottonAndroidBackgroundSyncScheduleStatus.NoEligibleRoot, result.Status);
            Assert.Null(result.Request);
            Assert.Empty(host.Requests);
            Assert.Equal("No sync folder is ready for Android background sync.", result.StatusText);
        }

        [Fact]
        public async Task Disabled_host_returns_unsupported_without_marking_request_scheduled()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, FolderId, "Projects");
            var coordinator = new CottonAndroidBackgroundSyncCoordinator(
                new FakeSyncRootStore([root]),
                new FakeSyncRootPauseStore(new HashSet<Guid>()),
                DisabledCottonAndroidBackgroundSyncHost.Instance);

            CottonAndroidBackgroundSyncScheduleResult result = await coordinator.ScheduleAsync(InstanceUri);

            Assert.Equal(CottonAndroidBackgroundSyncScheduleStatus.Unsupported, result.Status);
            Assert.False(result.IsScheduled);
            Assert.Equal("Android background sync is unavailable on this platform.", result.StatusText);
            Assert.NotNull(result.Request);
        }

        [Fact]
        public void Schedule_identity_is_stable_and_instance_scoped()
        {
            CottonAndroidBackgroundSyncScheduleIdentity first =
                CottonAndroidBackgroundSyncScheduleIdentity.Create(InstanceUri);
            CottonAndroidBackgroundSyncScheduleIdentity second =
                CottonAndroidBackgroundSyncScheduleIdentity.Create(InstanceUri);
            CottonAndroidBackgroundSyncScheduleIdentity other =
                CottonAndroidBackgroundSyncScheduleIdentity.Create(new Uri("https://files.cottoncloud.dev"));

            Assert.Equal(first.UniqueWorkName, second.UniqueWorkName);
            Assert.Equal(first.SyncTag, second.SyncTag);
            Assert.NotEqual(first.UniqueWorkName, other.UniqueWorkName);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            Guid folderId,
            string folderName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonSyncDirection direction = CottonSyncDirection.CloudToDevice)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    folderId,
                    folderName,
                    $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    $"app-private-sync-root-{folderId:N}",
                    "On this device",
                    permissionStatus),
                direction);
        }

        private class FakeBackgroundSyncHost : ICottonAndroidBackgroundSyncHost
        {
            public List<CottonAndroidBackgroundSyncRequest> Requests { get; } = [];

            public Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
                CottonAndroidBackgroundSyncRequest request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                return Task.FromResult(CottonAndroidBackgroundSyncScheduleResult.Scheduled(
                    request,
                    "Scheduled."));
            }
        }

        private class FakeSyncRootStore : ICottonSyncRootStore
        {
            private readonly IReadOnlyList<CottonSyncRootSnapshot> _roots;

            public FakeSyncRootStore(IReadOnlyList<CottonSyncRootSnapshot> roots)
            {
                _roots = roots;
            }

            public Task<IReadOnlyList<CottonSyncRootSnapshot>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_roots);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonSyncRootSnapshot> roots,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddOrReplaceAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> RemoveAsync(
                Uri instanceUri,
                Guid rootId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private class FakeSyncRootPauseStore : ICottonSyncRootPauseStore
        {
            private readonly IReadOnlySet<Guid> _pausedRootIds;

            public FakeSyncRootPauseStore(IReadOnlySet<Guid> pausedRootIds)
            {
                _pausedRootIds = pausedRootIds;
            }

            public Task<IReadOnlySet<Guid>> LoadPausedRootIdsAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_pausedRootIds);
            }

            public Task<bool> SetPausedAsync(
                Uri instanceUri,
                Guid rootId,
                bool isPaused,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
