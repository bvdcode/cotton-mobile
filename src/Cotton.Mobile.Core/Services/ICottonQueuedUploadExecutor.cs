namespace Cotton.Mobile.Services
{
    public interface ICottonQueuedUploadExecutor
    {
        Task<CottonQueuedUploadExecutionResult> ExecuteNextAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
