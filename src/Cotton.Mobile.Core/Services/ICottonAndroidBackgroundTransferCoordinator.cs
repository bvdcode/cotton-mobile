namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundTransferCoordinator
    {
        Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleNextQueuedUploadAsync(
            Uri instanceUri,
            int androidApiLevel,
            CancellationToken cancellationToken = default);
    }
}
