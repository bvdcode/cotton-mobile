namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidRemotePushTokenRefreshHost
    {
        Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CottonAndroidRemotePushTokenRefreshRequest request,
            CancellationToken cancellationToken = default);
    }
}
