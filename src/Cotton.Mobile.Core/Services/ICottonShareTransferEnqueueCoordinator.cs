namespace Cotton.Mobile.Services
{
    public interface ICottonShareTransferEnqueueCoordinator
    {
        Task<CottonShareTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
