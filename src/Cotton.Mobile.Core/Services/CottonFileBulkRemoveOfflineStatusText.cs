// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileBulkRemoveOfflineStatusText
    {
        public const string UnavailableStatus = "Selection is no longer available.";

        public static string CreateStartingStatus(int fileCount)
        {
            int count = NormalizeFileCount(fileCount);
            return count == 1 ? "Removing offline file..." : $"Removing {count} offline files...";
        }

        public static string CreateRemovingItemStatus(
            int itemPosition,
            int fileCount,
            string fileName)
        {
            int count = NormalizeFileCount(fileCount);
            int position = NormalizeItemPosition(itemPosition, count);
            string name = NormalizeFileName(fileName);

            return count == 1
                ? $"Removing {name} from this device..."
                : $"Removing {position} of {count} from this device: {name}...";
        }

        public static string CreateCompletedStatus(int removedCount)
        {
            int count = NormalizeFileCount(removedCount);
            return count == 1 ? "1 file removed from this device." : $"{count} files removed from this device.";
        }

        public static string CreateCancelledStatus(int removedCount, int fileCount)
        {
            return $"Remove offline cancelled after {FormatProgress(removedCount, fileCount)}.";
        }

        public static string CreateFailedStatus(int removedCount, int fileCount)
        {
            return $"Remove offline failed after {FormatProgress(removedCount, fileCount)}.";
        }

        private static string FormatProgress(int removedCount, int fileCount)
        {
            if (removedCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(removedCount),
                    "Removed count cannot be negative.");
            }

            int count = NormalizeFileCount(fileCount);
            if (removedCount > count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(removedCount),
                    "Removed count cannot exceed file count.");
            }

            return $"{removedCount}/{count} files";
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
