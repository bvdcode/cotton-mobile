using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class RecordingContentRevisionStore : ICottonContentRevisionStore
    {
        public List<CottonContentRevisionIndexSnapshot> SavedIndexes { get; } = [];

        public Task<CottonContentRevisionIndexSnapshot?> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CottonContentRevisionIndexSnapshot?>(SavedIndexes.LastOrDefault());
        }

        public Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonContentRevisionIndexSnapshot index,
            CancellationToken cancellationToken = default)
        {
            SavedIndexes.Add(index);
            return Task.CompletedTask;
        }

        public Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            SavedIndexes.Clear();
            return Task.CompletedTask;
        }
    }
}
