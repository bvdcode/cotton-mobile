namespace Cotton.Mobile.Services
{
    public interface ICottonTransferMetadataStore
    {
        Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonTransferQueueItem> items,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
