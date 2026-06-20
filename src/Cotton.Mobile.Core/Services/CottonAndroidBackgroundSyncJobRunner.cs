namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundSyncJobRunner : ICottonAndroidBackgroundSyncJobRunner
    {
        private readonly CottonCloudToDeviceSyncCoordinator _syncCoordinator;

        public CottonAndroidBackgroundSyncJobRunner(CottonCloudToDeviceSyncCoordinator syncCoordinator)
        {
            ArgumentNullException.ThrowIfNull(syncCoordinator);

            _syncCoordinator = syncCoordinator;
        }

        public Task<CottonCloudToDeviceSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            return _syncCoordinator.RunAsync(instanceUri, cancellationToken);
        }
    }
}
