namespace Cotton.Mobile.Services
{
    public interface ICottonQueuedUploadExecutor
    {
        Task<CottonQueuedUploadExecutionResult> ExecuteNextAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonQueuedUploadExecutionResult> ExecuteAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);
    }
}
