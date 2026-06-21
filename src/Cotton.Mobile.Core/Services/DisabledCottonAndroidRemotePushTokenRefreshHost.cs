namespace Cotton.Mobile.Services
{
    public class DisabledCottonAndroidRemotePushTokenRefreshHost : ICottonAndroidRemotePushTokenRefreshHost
    {
        public static DisabledCottonAndroidRemotePushTokenRefreshHost Instance { get; } = new();

        private DisabledCottonAndroidRemotePushTokenRefreshHost()
        {
        }

        public Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CottonAndroidRemotePushTokenRefreshRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                CottonAndroidRemotePushTokenRefreshScheduleResult.Unsupported(
                    request,
                    "Android remote push token refresh is unavailable on this platform."));
        }
    }
}
