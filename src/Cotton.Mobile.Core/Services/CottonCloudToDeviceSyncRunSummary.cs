// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncRunSummary
    {
        public CottonCloudToDeviceSyncRunSummary(IReadOnlyList<CottonCloudToDeviceSyncRootRunResult> rootResults)
        {
            ArgumentNullException.ThrowIfNull(rootResults);

            RootResults = rootResults;
        }

        public IReadOnlyList<CottonCloudToDeviceSyncRootRunResult> RootResults { get; }

        public int RootCount => RootResults.Count;

        public int CompletedRootCount => RootResults.Count(result => result.IsCompleted);

        public int SkippedRootCount => RootResults.Count(result => result.IsSkipped);

        public int DownloadedCount => RootResults.Sum(result => result.ExecutionResult?.DownloadedCount ?? 0);

        public int RefreshedCount => RootResults.Sum(result => result.ExecutionResult?.RefreshedCount ?? 0);

        public int RenamedCount => RootResults.Sum(result => result.ExecutionResult?.RenamedCount ?? 0);

        public int RemovedCount => RootResults.Sum(result => result.ExecutionResult?.RemovedCount ?? 0);

        public int SkippedItemCount => RootResults.Sum(result => result.ExecutionResult?.SkippedCount ?? 0);

        public int BlockedItemCount => RootResults.Sum(result => result.ExecutionResult?.BlockedCount ?? 0);

        public bool HasAppliedChanges => RootResults.Any(result => result.HasAppliedChanges);

        public bool HasBlockedItems => RootResults.Any(result => result.HasBlockedItems);

        public bool HasSkippedRoots => SkippedRootCount > 0;
    }
}
