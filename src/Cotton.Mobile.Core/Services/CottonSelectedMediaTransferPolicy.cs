namespace Cotton.Mobile.Services
{
    public static class CottonSelectedMediaTransferPolicy
    {
        public const string DirectForegroundMetadataValue = "directForeground";

        public const string QueueBackedMetadataValue = "queueBacked";

        public static string CurrentMetadataValue => DirectForegroundMetadataValue;

        public static bool UsesDurableQueue => false;

        public static bool RequiresDurableQueueBeforeCameraBackup => true;

        public static string ReleaseRiskText =>
            "Selected media imports upload immediately in the foreground. Durable queued retry/background upload is required before camera backup.";
    }
}
