namespace Cotton.Mobile.Services
{
    public static class CottonSelectedMediaUploadStatusText
    {
        public const string CancelledStatus = "Media import cancelled.";

        public const string FailedStatus = "Could not queue selected media.";

        public static string CreateQueueingStatus(string sourceKind, int count, string firstFileName)
        {
            string media = FormatMediaCount(sourceKind, count);
            string name = NormalizeFileName(firstFileName);
            return $"Queueing {media}: {name}...";
        }

        public static string CreateResultStatus(
            string sourceKind,
            CottonSelectedMediaTransferEnqueueResult result,
            CottonAndroidBackgroundTransferScheduleResult? scheduleResult)
        {
            ArgumentNullException.ThrowIfNull(result);

            string queueStatus = CreateQueuedStatus(sourceKind, result);
            if (!result.HasQueuedTransfers
                || scheduleResult is null
                || scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer)
            {
                return queueStatus;
            }

            if (scheduleResult.IsScheduled)
            {
                return $"{queueStatus} Android will upload in the background.";
            }

            if (scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.ForegroundRequired)
            {
                return $"{queueStatus} Open Transfers to run waiting uploads.";
            }

            if (scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.Unsupported)
            {
                return $"{queueStatus} Open Transfers to upload.";
            }

            return queueStatus;
        }

        private static string CreateQueuedStatus(
            string sourceKind,
            CottonSelectedMediaTransferEnqueueResult result)
        {
            if (!result.HasQueuedTransfers)
            {
                return $"No {FormatMediaNoun(sourceKind, plural: true)} selected.";
            }

            string media = FormatMediaCount(sourceKind, result.QueuedCount);
            string destination = result.Destination is null
                ? string.Empty
                : $" to {result.Destination.Path}";
            if (result.QueuedCount == 1)
            {
                string name = NormalizeFileName(result.FirstQueuedDisplayName);
                return $"Queued {media}: {name}{destination}.";
            }

            return $"Queued {media}{destination}.";
        }

        private static string FormatMediaCount(string sourceKind, int count)
        {
            int normalizedCount = Math.Max(1, count);
            string noun = FormatMediaNoun(sourceKind, plural: normalizedCount != 1);
            return $"{normalizedCount:N0} {noun}";
        }

        private static string FormatMediaNoun(string sourceKind, bool plural)
        {
            string normalized = string.IsNullOrWhiteSpace(sourceKind)
                ? CottonFileUploadStatusText.PhotoSourceKind
                : sourceKind.Trim().ToLowerInvariant();
            string noun = normalized == CottonFileUploadStatusText.VideoSourceKind ? "video" : "photo";
            return plural ? $"{noun}s" : noun;
        }

        private static string NormalizeFileName(string? fileName)
        {
            return string.IsNullOrWhiteSpace(fileName)
                ? CottonFileUploadSourceSnapshot.DefaultFileName
                : fileName.Trim();
        }
    }
}
