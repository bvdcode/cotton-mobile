namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundSyncScheduleResult
    {
        private CottonAndroidBackgroundSyncScheduleResult(
            CottonAndroidBackgroundSyncScheduleStatus status,
            CottonAndroidBackgroundSyncRequest? request,
            string statusText)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Background sync schedule status is not supported.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Background sync schedule status text is required.", nameof(statusText));
            }

            Status = status;
            Request = request;
            StatusText = statusText.Trim();
        }

        public CottonAndroidBackgroundSyncScheduleStatus Status { get; }

        public CottonAndroidBackgroundSyncRequest? Request { get; }

        public string StatusText { get; }

        public bool IsScheduled => Status == CottonAndroidBackgroundSyncScheduleStatus.Scheduled;

        public static CottonAndroidBackgroundSyncScheduleResult Scheduled(
            CottonAndroidBackgroundSyncRequest request,
            string statusText)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CottonAndroidBackgroundSyncScheduleResult(
                CottonAndroidBackgroundSyncScheduleStatus.Scheduled,
                request,
                statusText);
        }

        public static CottonAndroidBackgroundSyncScheduleResult Unsupported(
            CottonAndroidBackgroundSyncRequest request,
            string statusText)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CottonAndroidBackgroundSyncScheduleResult(
                CottonAndroidBackgroundSyncScheduleStatus.Unsupported,
                request,
                statusText);
        }

        public static CottonAndroidBackgroundSyncScheduleResult NoEligibleRoot()
        {
            return new CottonAndroidBackgroundSyncScheduleResult(
                CottonAndroidBackgroundSyncScheduleStatus.NoEligibleRoot,
                request: null,
                "No sync folder is ready for Android background sync.");
        }
    }
}
