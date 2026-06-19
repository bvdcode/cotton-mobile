namespace Cotton.Mobile.Services
{
    public static class CottonCameraBackupQueueStatusText
    {
        public static string CreateBlockedAccessStatus(CottonCameraBackupMediaAccessDisplayState mediaAccess)
        {
            ArgumentNullException.ThrowIfNull(mediaAccess);

            return mediaAccess.CanReadMedia
                ? "Automatic camera backup needs full media access."
                : "Allow media access before queueing camera backup.";
        }

        public static string CreateResultStatus(CottonCameraBackupTransferEnqueueResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.MissingDestination)
            {
                return "Choose a folder before queueing camera backup.";
            }

            if (result.QueuedCount == 1)
            {
                return "Queued 1 camera backup upload.";
            }

            if (result.QueuedCount > 1)
            {
                return $"Queued {result.QueuedCount:N0} camera backup uploads.";
            }

            if (result.MissingStreamCount == 1)
            {
                return "Could not read 1 camera item.";
            }

            if (result.MissingStreamCount > 1)
            {
                return $"Could not read {result.MissingStreamCount:N0} camera items.";
            }

            if (result.SkippedExistingTransferCount > 0)
            {
                return "Camera backup uploads are already queued.";
            }

            return "No new camera backup items to queue.";
        }
    }
}
