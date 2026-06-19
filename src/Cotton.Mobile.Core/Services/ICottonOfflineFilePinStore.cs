namespace Cotton.Mobile.Services
{
    public interface ICottonOfflineFilePinStore
    {
        Task<IReadOnlyList<CottonOfflineFilePinSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonOfflineFilePinSnapshot> items,
            CancellationToken cancellationToken = default);

        Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonOfflineFilePinSnapshot item,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default);

        Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
