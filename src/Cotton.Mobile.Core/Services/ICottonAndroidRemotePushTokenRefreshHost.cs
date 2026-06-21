namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidRemotePushTokenRefreshHost
    {
        Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CottonAndroidRemotePushTokenRefreshRequest request,
            CancellationToken cancellationToken = default);

        Task<CottonAndroidRemotePushTokenRefreshCancelResult> CancelAsync(
            CottonAndroidRemotePushTokenRefreshScheduleIdentity scheduleIdentity,
            CancellationToken cancellationToken = default);
    }
}
