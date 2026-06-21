namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshCoordinator
    {
        private readonly ICottonAndroidRemotePushTokenRefreshHost _host;

        public CottonAndroidRemotePushTokenRefreshCoordinator(
            ICottonAndroidRemotePushTokenRefreshHost host)
        {
            ArgumentNullException.ThrowIfNull(host);

            _host = host;
        }

        public async Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CancellationToken cancellationToken = default)
        {
            CottonAndroidRemotePushTokenRefreshRequest request =
                CottonAndroidRemotePushTokenRefreshRequest.CreateDefault();
            return await _host.ScheduleAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
