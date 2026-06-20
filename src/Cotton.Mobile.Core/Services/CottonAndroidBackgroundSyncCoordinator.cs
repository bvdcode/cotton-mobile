namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundSyncCoordinator : ICottonAndroidBackgroundSyncCoordinator
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonAndroidBackgroundSyncHost _host;

        public CottonAndroidBackgroundSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonAndroidBackgroundSyncHost host)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(host);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _host = host;
        }

        public async Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            int eligibleRootCount = roots.Count(root =>
                CottonCloudToDeviceSyncRootCapability.CanRun(root)
                && !pausedRootIds.Contains(root.Id));
            if (eligibleRootCount == 0)
            {
                return CottonAndroidBackgroundSyncScheduleResult.NoEligibleRoot();
            }

            var request = new CottonAndroidBackgroundSyncRequest(instanceUri, eligibleRootCount);
            return await _host.ScheduleAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
