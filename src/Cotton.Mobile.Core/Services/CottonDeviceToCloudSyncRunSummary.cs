// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncRunSummary
    {
        public CottonDeviceToCloudSyncRunSummary(
            IReadOnlyList<CottonDeviceToCloudSyncRootRunResult> rootResults)
        {
            ArgumentNullException.ThrowIfNull(rootResults);

            RootResults = rootResults;
        }

        public IReadOnlyList<CottonDeviceToCloudSyncRootRunResult> RootResults { get; }

        public int RootCount => RootResults.Count;

        public int CompletedRootCount => RootResults.Count(result => result.IsCompleted);

        public int SkippedRootCount => RootResults.Count(result => result.IsSkipped);

        public int UploadedCount => RootResults.Sum(result => result.ExecutionResult?.UploadedCount ?? 0);

        public int RefreshedCount => RootResults.Sum(result => result.ExecutionResult?.RefreshedCount ?? 0);

        public int CreatedFolderCount => RootResults.Sum(result => result.ExecutionResult?.CreatedFolderCount ?? 0);

        public int DeletedRemoteFileCount => RootResults.Sum(result => result.ExecutionResult?.DeletedRemoteFileCount ?? 0);

        public int RemovedManifestCount => RootResults.Sum(result => result.ExecutionResult?.RemovedManifestCount ?? 0);

        public int SkippedItemCount => RootResults.Sum(result => result.ExecutionResult?.SkippedCount ?? 0);

        public int BlockedItemCount => RootResults.Sum(result => result.ExecutionResult?.BlockedCount ?? 0);

        public int DestructiveReviewRemoteDeleteCount => RootResults
            .Where(result => result.Status == CottonDeviceToCloudSyncRootRunStatus.SkippedDestructiveReviewRequired)
            .Sum(result => result.Plan?.RemoteDeleteCount ?? 0);

        public bool HasAppliedChanges => RootResults.Any(result => result.HasAppliedChanges);

        public bool HasBlockedItems => RootResults.Any(result => result.HasBlockedItems);

        public bool HasSkippedRoots => SkippedRootCount > 0;

        public bool NeedsDestructiveReview => DestructiveReviewRemoteDeleteCount > 0;
    }
}
