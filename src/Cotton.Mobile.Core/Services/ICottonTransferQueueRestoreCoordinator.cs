namespace Cotton.Mobile.Services
{
    public interface ICottonTransferQueueRestoreCoordinator
    {
        Task<IReadOnlyList<CottonTransferQueueItem>> RestoreAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
