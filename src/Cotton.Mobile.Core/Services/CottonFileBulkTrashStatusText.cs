// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileBulkTrashStatusText
    {
        public const string CancelledStatus = "Move to trash cancelled.";

        public const string ConfirmAction = "Move to trash";

        public const string ConfirmTitle = "Move selection to trash?";

        public const string FailedStatus = "Could not move selection to trash.";

        public const string NeedsRefreshStatus = "Refresh this folder before moving selected files to trash.";

        public const string OfflineUnavailableStatus = "Offline. Move to trash needs internet.";

        public const string TimedOutStatus = "Move to trash is taking longer than expected. Refresh and try again.";

        public const string UnavailableStatus = "Selection is no longer available.";

        public static string CreateConfirmMessage(int fileCount, int folderCount)
        {
            string selectionText = CreateSelectionText(fileCount, folderCount);

            return $"{selectionText} will be removed from this folder and can be restored from trash.";
        }

        public static string CreateMovingStatus(int totalCount)
        {
            int count = NormalizeItemCount(totalCount);
            return count == 1 ? "Moving item to trash..." : $"Moving {count} items to trash...";
        }

        public static string CreateMovingItemStatus(
            int itemPosition,
            int totalCount,
            string itemName)
        {
            int count = NormalizeItemCount(totalCount);
            int position = NormalizeItemPosition(itemPosition, count);
            string name = NormalizeItemName(itemName);

            return count == 1
                ? $"Moving {name} to trash..."
                : $"Moving {position} of {count} to trash: {name}...";
        }

        public static string CreateMovedStatus(int movedCount)
        {
            int count = NormalizeItemCount(movedCount);
            return count == 1 ? "1 item moved to trash." : $"{count} items moved to trash.";
        }

        public static string CreateCancelledStatus(int movedCount, int totalCount)
        {
            return $"Move to trash cancelled after {FormatProgress(movedCount, totalCount)}.";
        }

        public static string CreateFailedStatus(int movedCount, int totalCount)
        {
            return $"Move to trash failed after {FormatProgress(movedCount, totalCount)}.";
        }

        private static string CreateSelectionText(int fileCount, int folderCount)
        {
            int files = NormalizeCount(fileCount, nameof(fileCount));
            int folders = NormalizeCount(folderCount, nameof(folderCount));

            if (files == 0 && folders == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "Selection must contain at least one item.");
            }

            var parts = new List<string>();
            if (files > 0)
            {
                parts.Add(files == 1 ? "1 file" : $"{files} files");
            }

            if (folders > 0)
            {
                parts.Add(folders == 1 ? "1 folder" : $"{folders} folders");
            }

            return string.Join(" and ", parts);
        }

        private static string FormatProgress(int movedCount, int totalCount)
        {
            if (movedCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movedCount),
                    "Moved count cannot be negative.");
            }

            int count = NormalizeItemCount(totalCount);
            if (movedCount > count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movedCount),
                    "Moved count cannot exceed item count.");
            }

            return $"{movedCount}/{count} items";
        }

        private static int NormalizeItemCount(int totalCount)
        {
            if (totalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), "Item count must be positive.");
            }

            return totalCount;
        }

        private static int NormalizeCount(int count, string parameterName)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Count cannot be negative.");
            }

            return count;
        }

        private static int NormalizeItemPosition(int itemPosition, int totalCount)
        {
            if (itemPosition <= 0 || itemPosition > totalCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemPosition),
                    "Item position must be inside item count.");
            }

            return itemPosition;
        }

        private static string NormalizeItemName(string itemName)
        {
            return string.IsNullOrWhiteSpace(itemName) ? "item" : itemName.Trim();
        }
    }
}
