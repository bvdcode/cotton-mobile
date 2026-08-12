// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFolderPagination
    {
        public const int PageSize = 100;
        public const int MaximumItemCount = CottonSyncTraversalGuard<Guid>.DefaultMaximumItemCount;

        public static int CreatePageCount(long totalCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
            if (totalCount > MaximumItemCount)
            {
                throw new InvalidDataException(
                    $"Cloud folder exceeds the maximum item count of {MaximumItemCount}.");
            }

            return checked((int)((totalCount + PageSize - 1) / PageSize));
        }

        public static void EnsureComplete(long expectedItemCount, int loadedItemCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(expectedItemCount);
            ArgumentOutOfRangeException.ThrowIfNegative(loadedItemCount);
            if (expectedItemCount != loadedItemCount)
            {
                throw new InvalidDataException(
                    $"Cloud folder returned {loadedItemCount} items instead of the declared {expectedItemCount}.");
            }
        }
    }
}
