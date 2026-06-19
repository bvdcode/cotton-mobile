namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupHealthDisplayState
    {
        private CottonCameraBackupHealthDisplayState(
            string title,
            string statusText,
            string countsText,
            bool isBlocked,
            bool hasActivity)
        {
            Title = title;
            StatusText = statusText;
            CountsText = countsText;
            IsBlocked = isBlocked;
            HasActivity = hasActivity;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string CountsText { get; }

        public bool IsBlocked { get; }

        public bool HasActivity { get; }

        public static CottonCameraBackupHealthDisplayState Create(
            CottonCameraBackupSettings settings,
            CottonCameraBackupHealthSnapshot health)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(health);

            string statusText = settings.CanRunBackup
                ? health.HasActivity ? "Backup activity is ready." : "No backup activity yet."
                : "Backup health will appear after background backup is available.";

            return new CottonCameraBackupHealthDisplayState(
                "Backup Health",
                statusText,
                $"Pending {health.PendingCount:N0} · Uploaded {health.UploadedCount:N0} · Failed {health.FailedCount:N0} · Blocked {health.BlockedCount:N0}",
                !settings.CanRunBackup,
                health.HasActivity);
        }
    }
}
