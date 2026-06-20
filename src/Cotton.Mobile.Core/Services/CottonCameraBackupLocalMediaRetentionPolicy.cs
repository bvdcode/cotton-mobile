namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupLocalMediaRetentionPolicy
    {
        private CottonCameraBackupLocalMediaRetentionPolicy(
            bool deletesSourceMedia,
            string setupSummaryText)
        {
            if (string.IsNullOrWhiteSpace(setupSummaryText))
            {
                throw new ArgumentException("Camera backup retention summary is required.", nameof(setupSummaryText));
            }

            DeletesSourceMedia = deletesSourceMedia;
            SetupSummaryText = setupSummaryText.Trim();
        }

        public static CottonCameraBackupLocalMediaRetentionPolicy Mvp { get; } = new(
            deletesSourceMedia: false,
            "Queue camera media to Cotton. Originals stay on this device.");

        public bool DeletesSourceMedia { get; }

        public bool RequiresMediaDeletePermission => DeletesSourceMedia;

        public string SetupSummaryText { get; }
    }
}
