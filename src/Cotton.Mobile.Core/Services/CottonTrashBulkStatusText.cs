// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonTrashBulkStatusText
    {
        public const string RestoreAction = "Restore";
        public const string DeleteForeverAction = "Delete forever";
        public const string RestoreTitle = "Restore selected items?";
        public const string DeleteForeverTitle = "Delete selected forever?";
        public const string CancelledStatus = "Selection action cancelled.";
        public const string RestoreFailedStatus = "Could not restore selected items.";
        public const string DeleteFailedStatus = "Could not delete selected items.";
        public const string RestoreOfflineUnavailableStatus = "Offline. Restore needs internet.";
        public const string DeleteOfflineUnavailableStatus = "Offline. Delete forever needs internet.";

        public static string CreateRestoreConfirmMessage(int fileCount, int folderCount)
        {
            ValidateSelectionCounts(fileCount, folderCount);

            return $"Restore {CreateSelectionCountText(fileCount + folderCount)} to their original locations?";
        }

        public static string CreateDeleteForeverConfirmMessage(int fileCount, int folderCount)
        {
            ValidateSelectionCounts(fileCount, folderCount);

            return $"Permanently delete {CreateSelectionCountText(fileCount + folderCount)}? This cannot be undone.";
        }

        public static string CreateRestoringStatus(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            return count == 1 ? "Restoring selected item..." : $"Restoring {count:N0} selected items...";
        }

        public static string CreateDeletingStatus(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            return count == 1 ? "Deleting selected item..." : $"Deleting {count:N0} selected items...";
        }

        public static string CreateRestoringItemStatus(int index, int count, string name)
        {
            ValidateProgress(index, count);

            return $"Restoring {index:N0} of {count:N0}: {NormalizeName(name)}";
        }

        public static string CreateDeletingItemStatus(int index, int count, string name)
        {
            ValidateProgress(index, count);

            return $"Deleting {index:N0} of {count:N0}: {NormalizeName(name)}";
        }

        public static string CreateRestoredStatus(int restoredCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(restoredCount);

            return restoredCount == 1
                ? "1 selected item restored."
                : $"{restoredCount:N0} selected items restored.";
        }

        public static string CreateDeletedStatus(int deletedCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deletedCount);

            return deletedCount == 1
                ? "1 selected item deleted forever."
                : $"{deletedCount:N0} selected items deleted forever.";
        }

        public static string CreatePartialRestoreStatus(int restoredCount, int totalCount)
        {
            ValidatePartial(restoredCount, totalCount);

            return $"{restoredCount:N0} of {totalCount:N0} selected items restored. Review remaining items.";
        }

        public static string CreatePartialDeleteStatus(int deletedCount, int totalCount)
        {
            ValidatePartial(deletedCount, totalCount);

            return $"{deletedCount:N0} of {totalCount:N0} selected items deleted forever. Review remaining items.";
        }

        private static string CreateSelectionCountText(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            return count == 1 ? "1 selected item" : $"{count:N0} selected items";
        }

        private static void ValidateSelectionCounts(int fileCount, int folderCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(fileCount);
            ArgumentOutOfRangeException.ThrowIfNegative(folderCount);

            if (fileCount + folderCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "At least one trash item is required.");
            }
        }

        private static void ValidateProgress(int index, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
            if (index > count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Progress index cannot exceed item count.");
            }
        }

        private static void ValidatePartial(int completedCount, int totalCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(completedCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalCount);
            if (completedCount >= totalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Partial count must be below total count.");
            }
        }

        private static string NormalizeName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "item" : name.Trim();
        }
    }
}
