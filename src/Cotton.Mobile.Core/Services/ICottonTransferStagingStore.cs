namespace Cotton.Mobile.Services
{
    public interface ICottonTransferStagingStore
    {
        Task<CottonTransferStagedFileSnapshot> StageAsync(
            Uri instanceUri,
            Guid transferId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default);

        Task<CottonTransferStagedFileSnapshot?> GetAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonTransferStagedFileSnapshot>> ListAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);

        Task CleanupAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonTransferQueueItem> queueItems,
            CancellationToken cancellationToken = default);
    }
}
