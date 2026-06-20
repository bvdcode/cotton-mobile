namespace Cotton.Mobile.Services
{
    public static class CottonFileBulkDownloadStatusText
    {
        public const string OfflineUnavailableStatus = "Offline. Download needs internet.";
        public const string UnavailableStatus = "Selection is no longer available.";

        public static string CreateStartingStatus(int fileCount)
        {
            int count = NormalizeFileCount(fileCount);
            return count == 1 ? "Downloading file..." : $"Downloading {count} files...";
        }

        public static string CreateDownloadingItemStatus(
            int itemPosition,
            int fileCount,
            string fileName)
        {
            int count = NormalizeFileCount(fileCount);
            int position = NormalizeItemPosition(itemPosition, count);
            string name = NormalizeFileName(fileName);

            return count == 1
                ? $"Downloading {name}..."
                : $"Downloading {position} of {count}: {name}...";
        }

        public static string CreateDownloadingItemProgressStatus(
            int itemPosition,
            int fileCount,
            string fileName,
            int percent)
        {
            int normalizedPercent = Math.Clamp(percent, 0, 100);
            return $"{CreateDownloadingItemStatus(itemPosition, fileCount, fileName)} {normalizedPercent}%";
        }

        public static string CreateCompletedStatus(int downloadedCount)
        {
            int count = NormalizeFileCount(downloadedCount);
            return count == 1 ? "1 file downloaded." : $"{count} files downloaded.";
        }

        public static string CreateCancelledStatus(int downloadedCount, int fileCount)
        {
            return $"Download cancelled after {FormatProgress(downloadedCount, fileCount)}.";
        }

        public static string CreateFailedStatus(int downloadedCount, int fileCount)
        {
            return $"Download failed after {FormatProgress(downloadedCount, fileCount)}.";
        }

        private static string FormatProgress(int downloadedCount, int fileCount)
        {
            if (downloadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(downloadedCount),
                    "Downloaded count cannot be negative.");
            }

            int count = NormalizeFileCount(fileCount);
            if (downloadedCount > count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(downloadedCount),
                    "Downloaded count cannot exceed file count.");
            }

            return $"{downloadedCount}/{count} files";
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
