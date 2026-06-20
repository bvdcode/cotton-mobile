namespace Cotton.Mobile.Services
{
    public enum CottonSyncConflictResolutionAction
    {
        Refresh,
        KeepLocalChange,
        UseCloudVersion,
        SkipLocalChange,
        ReconnectLocalRoot,
        Dismiss,
    }
}
