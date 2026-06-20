namespace Cotton.Mobile.Services
{
    public interface ICottonSyncedFileManifestStore
    {
        Task<IReadOnlyList<CottonSyncedFileSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlyCollection<CottonSyncedFileSnapshot> items,
            CancellationToken cancellationToken = default);

        Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonSyncedFileSnapshot item,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            Guid fileId,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);
    }
}
