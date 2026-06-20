namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationLaunchState
    {
        event EventHandler? NotificationLaunchRequested;

        int PendingNotificationLaunchCount { get; }

        void NotifyNotificationOpened(CottonNotificationLaunchRequest request);

        CottonNotificationLaunchRequest? TryConsumePendingNotificationLaunch();

        void ClearPendingNotificationLaunches();
    }
}
