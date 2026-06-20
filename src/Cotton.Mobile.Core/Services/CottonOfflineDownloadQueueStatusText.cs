namespace Cotton.Mobile.Services
{
    public static class CottonOfflineDownloadQueueStatusText
    {
        public static string CreateQueuedStatus(CottonOfflineDownloadQueueSnapshot queue)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return $"Queued {FormatFileCount(queue.TotalCount)} for offline use ({CottonFileSizeFormatter.Format(queue.TotalSizeBytes)}).";
        }

        public static string CreateStartingItemStatus(
            CottonOfflineDownloadQueueItem item,
            int totalCount)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (totalCount <= 0 || item.Position > totalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count must include the item position.");
            }

            return $"Saving {item.Position} of {totalCount}: {item.DisplayName}...";
        }

        public static string CreateCompletedStatus(CottonOfflineDownloadQueueSnapshot queue)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return $"{FormatFileCount(queue.TotalCount)} available offline from {queue.FolderName}.";
        }

        public static string CreateCancelledStatus(int completedCount, int totalCount)
        {
            return $"Keep folder offline cancelled after {FormatProgress(completedCount, totalCount)}.";
        }

        public static string CreateFailedStatus(int completedCount, int totalCount)
        {
            return $"Keep folder offline failed after {FormatProgress(completedCount, totalCount)}.";
        }

        public static string CreateFailureDetail(bool hasInternetAccess)
        {
            return hasInternetAccess
                ? "Download failed."
                : CottonOfflineFolderStatusText.OfflineUnavailableStatus;
        }

        private static string FormatProgress(int completedCount, int totalCount)
        {
            if (completedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Completed count cannot be negative.");
            }

            if (totalCount <= 0 || completedCount > totalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count must include completed count.");
            }

            return $"{completedCount}/{totalCount} files";
        }

        private static string FormatFileCount(int count)
        {
            return count == 1 ? "1 file" : $"{count} files";
        }
    }
}
