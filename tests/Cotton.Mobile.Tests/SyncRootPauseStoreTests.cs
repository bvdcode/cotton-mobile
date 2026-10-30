using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootPauseStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid OtherRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootPauseStore _store;

        public SyncRootPauseStoreTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-sync-root-pause-store-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootPauseStore>.Instance, TimeProvider.System);
        }

        [Fact]
        public async Task LoadReturnsEmptySetWhenMetadataIsMissing()
        {
            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.Empty(pausedRootIds);
        }

        [Fact]
        public async Task SetPausedAddsAndRemovesRootId()
        {
            bool added = await _store.SetPausedAsync(InstanceUri, RootId, isPaused: true);
            bool duplicateAdd = await _store.SetPausedAsync(InstanceUri, RootId, isPaused: true);

            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.True(added);
            Assert.False(duplicateAdd);
            Assert.Contains(RootId, pausedRootIds);

            bool removed = await _store.SetPausedAsync(InstanceUri, RootId, isPaused: false);
            bool duplicateRemove = await _store.SetPausedAsync(InstanceUri, RootId, isPaused: false);

            Assert.True(removed);
            Assert.False(duplicateRemove);
            Assert.Empty(await _store.LoadPausedRootIdsAsync(InstanceUri));
            Assert.False(File.Exists(CreateMetadataPath()));
        }

        [Fact]
        public async Task LoadFiltersEmptyAndDuplicateRootIds()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                $$"""
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-20T18:00:00Z",
                  "rootIds": [
                    "00000000-0000-0000-0000-000000000000",
                    "{{RootId:D}}",
                    "{{RootId:D}}",
                    "{{OtherRootId:D}}"
                  ]
                }
                """);

            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.Equal(2, pausedRootIds.Count);
            Assert.Contains(RootId, pausedRootIds);
            Assert.Contains(OtherRootId, pausedRootIds);
        }

        [Fact]
        public async Task LoadDeletesCorruptMetadataAndReturnsEmpty()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(CreateMetadataPath(), "{ not valid json");

            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.Empty(pausedRootIds);
            Assert.False(File.Exists(CreateMetadataPath()));
        }

        [Fact]
        public async Task SavePropagatesFileSystemFailures()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
            await File.WriteAllTextAsync(_directory, "blocked directory");

            await Assert.ThrowsAnyAsync<IOException>(() =>
                _store.SetPausedAsync(InstanceUri, RootId, isPaused: true));
        }

        [Fact]
        public async Task ClearRemovesPauseMetadata()
        {
            await _store.SetPausedAsync(InstanceUri, RootId, isPaused: true);

            await _store.ClearAsync(InstanceUri);

            Assert.Empty(await _store.LoadPausedRootIdsAsync(InstanceUri));
            Assert.False(File.Exists(CreateMetadataPath()));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            if (File.Exists(_directory))
            {
                File.Delete(_directory);
            }

            GC.SuppressFinalize(this);
        }

        private string CreateMetadataPath()
        {
            return Path.Combine(_directory, FileSystemCottonSyncRootPauseStore.MetadataFileName);
        }
    }
}
