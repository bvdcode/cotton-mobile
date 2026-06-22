// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncExecutionResult
    {
        public CottonCloudToDeviceSyncExecutionResult(
            int downloadedCount,
            int refreshedCount,
            int renamedCount,
            int removedCount,
            int skippedCount,
            int blockedCount)
        {
            if (downloadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(downloadedCount), "Downloaded count cannot be negative.");
            }

            if (refreshedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshedCount), "Refreshed count cannot be negative.");
            }

            if (renamedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(renamedCount), "Renamed count cannot be negative.");
            }

            if (removedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(removedCount), "Removed count cannot be negative.");
            }

            if (skippedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedCount), "Skipped count cannot be negative.");
            }

            if (blockedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockedCount), "Blocked count cannot be negative.");
            }

            DownloadedCount = downloadedCount;
            RefreshedCount = refreshedCount;
            RenamedCount = renamedCount;
            RemovedCount = removedCount;
            SkippedCount = skippedCount;
            BlockedCount = blockedCount;
        }

        public int DownloadedCount { get; }

        public int RefreshedCount { get; }

        public int RenamedCount { get; }

        public int RemovedCount { get; }

        public int SkippedCount { get; }

        public int BlockedCount { get; }

        public bool HasAppliedChanges => DownloadedCount + RefreshedCount + RenamedCount + RemovedCount > 0;

        public bool HasBlockedItems => BlockedCount > 0;
    }
}
