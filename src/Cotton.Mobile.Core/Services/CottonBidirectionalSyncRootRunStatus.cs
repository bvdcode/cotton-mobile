namespace Cotton.Mobile.Services
{
    public enum CottonBidirectionalSyncRootRunStatus
    {
        Completed,
        SkippedPaused,
        SkippedNotReady,
        SkippedUnsupportedLocalRoot,
        SkippedUnsupportedDirection,
        SkippedConflictReviewRequired,
        SkippedBlockedReviewRequired,
        SkippedDestructiveReviewRequired,
    }
}
