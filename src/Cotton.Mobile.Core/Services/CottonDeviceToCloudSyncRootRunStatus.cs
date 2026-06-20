namespace Cotton.Mobile.Services
{
    public enum CottonDeviceToCloudSyncRootRunStatus
    {
        Completed,
        SkippedPaused,
        SkippedNotReady,
        SkippedUnsupportedLocalRoot,
        SkippedUnsupportedDirection,
        SkippedDestructiveReviewRequired,
    }
}
