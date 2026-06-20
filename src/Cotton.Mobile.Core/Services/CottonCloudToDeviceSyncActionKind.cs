namespace Cotton.Mobile.Services
{
    public enum CottonCloudToDeviceSyncActionKind
    {
        DownloadNewFile,
        RefreshChangedFile,
        RenameLocalFile,
        KeepExistingFile,
        RemoveLocalOrphan,
        BlockedFolder,
        NeedsFreshServerRevision,
    }
}
