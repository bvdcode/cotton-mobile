namespace Cotton.Mobile.Services
{
    public interface ICottonTrashRestoreService
    {
        Task<CottonTrashRestoreResult> RestoreAsync(
            Uri instanceUri,
            Guid itemId,
            CottonFileBrowserEntryType itemType,
            CottonTrashRestoreRetryMode retryMode = CottonTrashRestoreRetryMode.None,
            CancellationToken cancellationToken = default);
    }
}
