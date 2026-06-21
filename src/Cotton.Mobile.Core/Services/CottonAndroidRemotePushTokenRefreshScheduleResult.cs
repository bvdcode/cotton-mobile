namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshScheduleResult
    {
        private CottonAndroidRemotePushTokenRefreshScheduleResult(
            CottonAndroidRemotePushTokenRefreshScheduleStatus status,
            CottonAndroidRemotePushTokenRefreshRequest request,
            string statusText)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Remote push token refresh schedule status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Remote push token refresh schedule status text is required.", nameof(statusText));
            }

            Status = status;
            Request = request;
            StatusText = statusText.Trim();
        }

        public CottonAndroidRemotePushTokenRefreshScheduleStatus Status { get; }

        public CottonAndroidRemotePushTokenRefreshRequest Request { get; }

        public string StatusText { get; }

        public bool IsScheduled => Status == CottonAndroidRemotePushTokenRefreshScheduleStatus.Scheduled;

        public static CottonAndroidRemotePushTokenRefreshScheduleResult Scheduled(
            CottonAndroidRemotePushTokenRefreshRequest request,
            string statusText)
        {
            return new CottonAndroidRemotePushTokenRefreshScheduleResult(
                CottonAndroidRemotePushTokenRefreshScheduleStatus.Scheduled,
                request,
                statusText);
        }

        public static CottonAndroidRemotePushTokenRefreshScheduleResult Unsupported(
            CottonAndroidRemotePushTokenRefreshRequest request,
            string statusText)
        {
            return new CottonAndroidRemotePushTokenRefreshScheduleResult(
                CottonAndroidRemotePushTokenRefreshScheduleStatus.Unsupported,
                request,
                statusText);
        }
    }
}
