namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncRunSummary
    {
        public CottonBidirectionalSyncRunSummary(
            IReadOnlyList<CottonBidirectionalSyncRootRunResult> rootResults)
        {
            ArgumentNullException.ThrowIfNull(rootResults);

            RootResults = rootResults;
        }

        public IReadOnlyList<CottonBidirectionalSyncRootRunResult> RootResults { get; }

        public int RootCount => RootResults.Count;

        public int CompletedRootCount => RootResults.Count(result => result.IsCompleted);

        public int SkippedRootCount => RootResults.Count(result => result.IsSkipped);

        public int DownloadedCount => RootResults.Sum(result => result.CloudToDeviceExecutionResult?.DownloadedCount ?? 0);

        public int RefreshedLocalCount => RootResults.Sum(result => result.CloudToDeviceExecutionResult?.RefreshedCount ?? 0);

        public int RenamedLocalCount => RootResults.Sum(result => result.CloudToDeviceExecutionResult?.RenamedCount ?? 0);

        public int RemovedLocalCount => RootResults.Sum(result => result.CloudToDeviceExecutionResult?.RemovedCount ?? 0);

        public int UploadedCount => RootResults.Sum(result => result.DeviceToCloudExecutionResult?.UploadedCount ?? 0);

        public int RefreshedRemoteCount => RootResults.Sum(result => result.DeviceToCloudExecutionResult?.RefreshedCount ?? 0);

        public int CreatedFolderCount => RootResults.Sum(result => result.DeviceToCloudExecutionResult?.CreatedFolderCount ?? 0);

        public int DeletedRemoteFileCount => RootResults.Sum(result => result.DeviceToCloudExecutionResult?.DeletedRemoteFileCount ?? 0);

        public int RemovedManifestCount => RootResults.Sum(result => result.DeviceToCloudExecutionResult?.RemovedManifestCount ?? 0);

        public int BlockedItemCount => RootResults.Sum(result =>
            (result.CloudToDeviceExecutionResult?.BlockedCount ?? 0)
            + (result.DeviceToCloudExecutionResult?.BlockedCount ?? 0));

        public int ConflictReviewCount => RootResults
            .Where(result => result.Status == CottonBidirectionalSyncRootRunStatus.SkippedConflictReviewRequired)
            .Sum(result => result.PreflightPlan?.ConflictCount ?? 0);

        public int DestructiveReviewLocalDeleteCount => RootResults
            .Where(result => result.Status == CottonBidirectionalSyncRootRunStatus.SkippedDestructiveReviewRequired)
            .Sum(result => result.PreflightPlan?.LocalDeleteCount ?? 0);

        public int DestructiveReviewRemoteDeleteCount => RootResults
            .Where(result => result.Status == CottonBidirectionalSyncRootRunStatus.SkippedDestructiveReviewRequired)
            .Sum(result => result.PreflightPlan?.RemoteDeleteCount ?? 0);

        public bool HasAppliedChanges => RootResults.Any(result => result.HasAppliedChanges);

        public bool HasBlockedItems => RootResults.Any(result => result.HasBlockedItems);

        public bool HasSkippedRoots => SkippedRootCount > 0;

        public bool NeedsConflictReview => ConflictReviewCount > 0;

        public bool NeedsDestructiveReview =>
            DestructiveReviewLocalDeleteCount + DestructiveReviewRemoteDeleteCount > 0;
    }
}
