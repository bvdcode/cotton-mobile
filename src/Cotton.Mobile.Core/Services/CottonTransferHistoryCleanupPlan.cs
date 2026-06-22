// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferHistoryCleanupPlan
    {
        public CottonTransferHistoryCleanupPlan(
            IReadOnlyList<CottonTransferQueueItem> retainedItems,
            int removedCount)
        {
            ArgumentNullException.ThrowIfNull(retainedItems);
            if (removedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(removedCount), "Removed transfer count cannot be negative.");
            }

            RetainedItems = retainedItems;
            RemovedCount = removedCount;
        }

        public IReadOnlyList<CottonTransferQueueItem> RetainedItems { get; }

        public int RemovedCount { get; }

        public int RemainingCount => RetainedItems.Count;

        public bool HasRemovedItems => RemovedCount > 0;
    }
}
