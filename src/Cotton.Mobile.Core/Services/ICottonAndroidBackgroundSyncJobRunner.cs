namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundSyncJobRunner
    {
        Task<CottonCloudToDeviceSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
