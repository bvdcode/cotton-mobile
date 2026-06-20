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
            _store = new FileSystemCottonSyncRootPauseStore(new FixedSyncRootMetadataPathProvider(_directory));
        }

        [Fact]
        public async Task Load_returns_empty_set_when_metadata_is_missing()
        {
            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.Empty(pausedRootIds);
        }

        [Fact]
        public async Task Set_paused_adds_and_removes_root_id()
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
        public async Task Load_filters_empty_and_duplicate_root_ids()
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
        public async Task Load_deletes_corrupt_metadata_and_returns_empty()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(CreateMetadataPath(), "{ not valid json");

            IReadOnlySet<Guid> pausedRootIds = await _store.LoadPausedRootIdsAsync(InstanceUri);

            Assert.Empty(pausedRootIds);
            Assert.False(File.Exists(CreateMetadataPath()));
        }

        [Fact]
        public async Task Clear_removes_pause_metadata()
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
        }

        private string CreateMetadataPath()
        {
            return Path.Combine(_directory, FileSystemCottonSyncRootPauseStore.MetadataFileName);
        }

        private class FixedSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
        {
            private readonly string _directory;

            public FixedSyncRootMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncRootMetadataDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }
    }
}
