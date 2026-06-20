namespace Cotton.Mobile.Services
{
    public interface ICottonSyncRootStore
    {
        Task<IReadOnlyList<CottonSyncRootSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonSyncRootSnapshot> roots,
            CancellationToken cancellationToken = default);

        Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid rootId,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
