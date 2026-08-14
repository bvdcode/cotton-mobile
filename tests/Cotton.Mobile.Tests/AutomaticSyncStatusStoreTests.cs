using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncStatusStoreTests : IDisposable
    {
        private static readonly Guid FirstRootId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondRootId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private readonly string _directory;
        private readonly FileSystemCottonAutomaticSyncStatusStore _store;

        public AutomaticSyncStatusStoreTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-automatic-sync-status-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonAutomaticSyncStatusStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonAutomaticSyncStatusStore>.Instance,
                new FixedTimeProvider(new DateTime(2026, 8, 14, 18, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public async Task UpdateMergesSelectedRootsAndKeepsOtherActiveStatuses()
        {
            DateTime firstAttempt = new(2026, 8, 14, 17, 0, 0, DateTimeKind.Utc);
            DateTime retryAttempt = new(2026, 8, 14, 17, 30, 0, DateTimeKind.Utc);
            HashSet<Guid> activeRootIds = [FirstRootId, SecondRootId];
            await _store.UpdateAsync(
                SyncTestRootFactory.InstanceUri,
                activeRootIds,
                [
                    new CottonAutomaticSyncRootStatusSnapshot(
                        FirstRootId,
                        CottonAutomaticSyncOutcome.Failed,
                        firstAttempt),
                    new CottonAutomaticSyncRootStatusSnapshot(
                        SecondRootId,
                        CottonAutomaticSyncOutcome.Succeeded,
                        firstAttempt),
                ]);

            await _store.UpdateAsync(
                SyncTestRootFactory.InstanceUri,
                activeRootIds,
                [
                    new CottonAutomaticSyncRootStatusSnapshot(
                        FirstRootId,
                        CottonAutomaticSyncOutcome.Succeeded,
                        retryAttempt),
                ]);

            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _store.LoadAsync(SyncTestRootFactory.InstanceUri);
            Assert.Equal(2, statuses.Count);
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, statuses[FirstRootId].Outcome);
            Assert.Equal(retryAttempt, statuses[FirstRootId].CompletedAtUtc);
            Assert.Equal(firstAttempt, statuses[SecondRootId].CompletedAtUtc);
        }

        [Fact]
        public async Task UpdateRemovesStatusesForDeletedRoots()
        {
            DateTime attempt = new(2026, 8, 14, 17, 0, 0, DateTimeKind.Utc);
            await _store.UpdateAsync(
                SyncTestRootFactory.InstanceUri,
                new HashSet<Guid> { FirstRootId, SecondRootId },
                [
                    new CottonAutomaticSyncRootStatusSnapshot(
                        FirstRootId,
                        CottonAutomaticSyncOutcome.Succeeded,
                        attempt),
                    new CottonAutomaticSyncRootStatusSnapshot(
                        SecondRootId,
                        CottonAutomaticSyncOutcome.Failed,
                        attempt),
                ]);

            await _store.UpdateAsync(
                SyncTestRootFactory.InstanceUri,
                new HashSet<Guid> { FirstRootId },
                []);

            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses =
                await _store.LoadAsync(SyncTestRootFactory.InstanceUri);
            Assert.Equal([FirstRootId], statuses.Keys);
        }

        [Fact]
        public async Task UpdatePublishesResultingStatuses()
        {
            CottonAutomaticSyncStatusesChangedEventArgs? eventArgs = null;
            _store.StatusesChanged += (_, args) => eventArgs = args;
            DateTime attempt = new(2026, 8, 14, 17, 0, 0, DateTimeKind.Utc);

            await _store.UpdateAsync(
                SyncTestRootFactory.InstanceUri,
                new HashSet<Guid> { FirstRootId },
                [
                    new CottonAutomaticSyncRootStatusSnapshot(
                        FirstRootId,
                        CottonAutomaticSyncOutcome.Succeeded,
                        attempt),
                ]);

            Assert.NotNull(eventArgs);
            Assert.Equal(SyncTestRootFactory.InstanceUri, eventArgs.InstanceUri);
            Assert.Equal(CottonAutomaticSyncOutcome.Succeeded, eventArgs.Statuses[FirstRootId].Outcome);
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
    }
}
