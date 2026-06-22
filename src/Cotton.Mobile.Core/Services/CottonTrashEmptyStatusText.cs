// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonTrashEmptyStatusText
    {
        public const string ConfirmAction = "Empty trash";

        public const string ConfirmTitle = "Empty trash?";

        public const string CancelledStatus = "Empty trash cancelled.";

        public const string FailedStatus = "Could not empty trash.";

        public const string OfflineUnavailableStatus = "Offline. Empty trash needs internet.";

        public static string CreateConfirmMessage(int itemCount)
        {
            EnsurePositive(itemCount, nameof(itemCount));

            return itemCount == 1
                ? "Permanently delete 1 trash item? This cannot be undone."
                : $"Permanently delete all {itemCount:N0} trash items? This cannot be undone.";
        }

        public static string CreateDeletingStatus(int itemCount)
        {
            EnsurePositive(itemCount, nameof(itemCount));

            return itemCount == 1 ? "Deleting trash item..." : $"Deleting {itemCount:N0} trash items...";
        }

        public static string CreateDeletingItemStatus(int itemIndex, int totalCount, string name)
        {
            EnsureItemIndex(itemIndex, totalCount);

            string displayName = string.IsNullOrWhiteSpace(name) ? "item" : name.Trim();
            return $"Deleting {itemIndex:N0} of {totalCount:N0}: {displayName}";
        }

        public static string CreateDeletedStatus(int itemCount)
        {
            EnsurePositive(itemCount, nameof(itemCount));

            return itemCount == 1 ? "1 trash item deleted forever." : $"{itemCount:N0} trash items deleted forever.";
        }

        public static string CreatePartialDeleteStatus(int deletedCount, int totalCount)
        {
            EnsurePositive(totalCount, nameof(totalCount));
            if (deletedCount <= 0 || deletedCount >= totalCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deletedCount),
                    "Partial delete count must be greater than zero and less than the total count.");
            }

            return $"{deletedCount:N0} of {totalCount:N0} trash items deleted forever. Review remaining items.";
        }

        private static void EnsurePositive(int count, string parameterName)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Trash item count must be positive.");
            }
        }

        private static void EnsureItemIndex(int itemIndex, int totalCount)
        {
            EnsurePositive(totalCount, nameof(totalCount));
            if (itemIndex <= 0 || itemIndex > totalCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemIndex),
                    "Trash item index must be within the total count.");
            }
        }
    }
}
