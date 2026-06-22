// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonTransferStagedFileCleanupPolicy
    {
        public static IReadOnlySet<Guid> ResolveTransferIdsToDelete(
            IReadOnlyCollection<CottonTransferQueueItem> queueItems,
            IReadOnlyCollection<Guid> stagedTransferIds)
        {
            ArgumentNullException.ThrowIfNull(queueItems);
            ArgumentNullException.ThrowIfNull(stagedTransferIds);

            Dictionary<Guid, CottonTransferQueueItem> queueById = queueItems.ToDictionary(item => item.Id);
            return stagedTransferIds
                .Where(stagedTransferId =>
                    !queueById.TryGetValue(stagedTransferId, out CottonTransferQueueItem? item)
                    || item.Status is CottonTransferStatus.Completed or CottonTransferStatus.Cancelled)
                .ToHashSet();
        }
    }
}
