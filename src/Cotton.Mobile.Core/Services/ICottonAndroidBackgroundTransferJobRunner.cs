namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundTransferJobRunner
    {
        Task<CottonQueuedUploadExecutionResult> RunAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);
    }
}
