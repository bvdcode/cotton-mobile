namespace Cotton.Mobile.Services
{
    public interface ICottonSyncRootPauseStore
    {
        Task<IReadOnlySet<Guid>> LoadPausedRootIdsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<bool> SetPausedAsync(
            Uri instanceUri,
            Guid rootId,
            bool isPaused,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
