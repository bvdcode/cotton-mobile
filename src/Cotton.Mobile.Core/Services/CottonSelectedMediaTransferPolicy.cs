namespace Cotton.Mobile.Services
{
    public static class CottonSelectedMediaTransferPolicy
    {
        public const string DirectForegroundMetadataValue = "directForeground";

        public const string QueueBackedMetadataValue = "queueBacked";

        public static string CurrentMetadataValue => QueueBackedMetadataValue;

        public static bool UsesDurableQueue => true;

        public static bool RequiresDurableQueueBeforeCameraBackup => false;

        public static string ReleaseRiskText =>
            "Selected media imports are staged into the durable transfer queue before upload.";
    }
}
