namespace Cotton.Mobile.Services
{
    public interface ICottonQueuedUploadClient
    {
        Task<CottonQueuedUploadClientResult> UploadAsync(
            Uri instanceUri,
            CottonTransferQueueItem transfer,
            CottonTransferStagedFileSnapshot stagedFile,
            Func<long, CancellationToken, Task> reportProgressAsync,
            CancellationToken cancellationToken = default);
    }
}
