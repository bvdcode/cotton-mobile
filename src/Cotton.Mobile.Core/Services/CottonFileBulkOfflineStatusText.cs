namespace Cotton.Mobile.Services
{
    public static class CottonFileBulkOfflineStatusText
    {
        public const string OfflineUnavailableStatus = "Offline. Keep offline needs internet.";
        public const string UnavailableStatus = "Selection is no longer available.";

        public static string CreateStartingStatus(int fileCount)
        {
            int count = NormalizeFileCount(fileCount);
            return count == 1 ? "Keeping file offline..." : $"Keeping {count} files offline...";
        }

        public static string CreateSavingItemStatus(
            int itemPosition,
            int fileCount,
            string fileName)
        {
            int count = NormalizeFileCount(fileCount);
            int position = NormalizeItemPosition(itemPosition, count);
            string name = NormalizeFileName(fileName);

            return count == 1
                ? $"Keeping {name} offline..."
                : $"Keeping {position} of {count} offline: {name}...";
        }

        public static string CreateSavingItemProgressStatus(
            int itemPosition,
            int fileCount,
            string fileName,
            int percent)
        {
            int normalizedPercent = Math.Clamp(percent, 0, 100);
            return $"{CreateSavingItemStatus(itemPosition, fileCount, fileName)} {normalizedPercent}%";
        }

        public static string CreateCompletedStatus(int completedCount)
        {
            int count = NormalizeFileCount(completedCount);
            return count == 1 ? "1 file available offline." : $"{count} files available offline.";
        }

        public static string CreateCancelledStatus(int completedCount, int fileCount)
        {
            return $"Keep offline cancelled after {FormatProgress(completedCount, fileCount)}.";
        }

        public static string CreateFailedStatus(int completedCount, int fileCount)
        {
            return $"Keep offline failed after {FormatProgress(completedCount, fileCount)}.";
        }

        private static string FormatProgress(int completedCount, int fileCount)
        {
            if (completedCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedCount),
                    "Completed count cannot be negative.");
            }

            int count = NormalizeFileCount(fileCount);
            if (completedCount > count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedCount),
                    "Completed count cannot exceed file count.");
            }

            return $"{completedCount}/{count} files";
        }

        private static int NormalizeFileCount(int fileCount)
        {
            if (fileCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "File count must be positive.");
            }

            return fileCount;
        }

        private static int NormalizeItemPosition(int itemPosition, int fileCount)
        {
            if (itemPosition <= 0 || itemPosition > fileCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemPosition),
                    "Item position must be inside file count.");
            }

            return itemPosition;
        }

        private static string NormalizeFileName(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? "file" : fileName.Trim();
        }
    }
}
