using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ContentRevisionStoreTests : IDisposable
    {
        private readonly string _directory;
        private readonly CottonSyncRootSnapshot _root;
        private readonly FileSystemCottonContentRevisionStore _store;

        public ContentRevisionStoreTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-content-revisions",
                Guid.NewGuid().ToString("N"));
            _root = SyncTestRootFactory.CreateMediaStoreRoot();
            _store = new FileSystemCottonContentRevisionStore(
                new FixedContentRevisionPathProvider(_directory));
        }

        [Fact]
        public async Task MissingIndexReturnsNull()
        {
            CottonContentRevisionIndexSnapshot? index = await _store.LoadAsync(
                SyncTestRootFactory.InstanceUri,
                _root);

            Assert.Null(index);
        }

        [Fact]
        public async Task SavedIndexRoundTrips()
        {
            CottonContentRevisionIndexSnapshot expected = CreateIndex();

            await _store.SaveAsync(SyncTestRootFactory.InstanceUri, _root, expected);
            CottonContentRevisionIndexSnapshot? actual = await _store.LoadAsync(
                SyncTestRootFactory.InstanceUri,
                _root);

            Assert.NotNull(actual);
            Assert.True(expected.HasSameContentAs(actual));
        }

        [Fact]
        public async Task ClearRemovesSavedIndex()
        {
            await _store.SaveAsync(SyncTestRootFactory.InstanceUri, _root, CreateIndex());

            await _store.ClearAsync(SyncTestRootFactory.InstanceUri, _root);

            Assert.Null(await _store.LoadAsync(SyncTestRootFactory.InstanceUri, _root));
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

        private static CottonContentRevisionIndexSnapshot CreateIndex()
        {
            return new CottonContentRevisionIndexSnapshot(
                "version-1",
                [new CottonContentRevisionSnapshot(
                    "content://media/1",
                    12,
                    TestContentHashes.First,
                    sizeBytes: 42)]);
        }
    }
}
