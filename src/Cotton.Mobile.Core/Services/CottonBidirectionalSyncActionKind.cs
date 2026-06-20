namespace Cotton.Mobile.Services
{
    public enum CottonBidirectionalSyncActionKind
    {
        KeepExistingFile,
        KeepExistingFolder,
        DownloadNewFile,
        RefreshLocalFile,
        RenameLocalFile,
        RemoveLocalFile,
        UploadNewFile,
        UploadChangedFile,
        CreateRemoteFolder,
        DeleteRemoteFile,
        RemoveManifestOrphan,
        FileChangedOnBothSides,
        RemotePathConflict,
        RemoteTargetMissing,
        NeedsFreshServerRevision,
        BlockedLocalItemName,
        BlockedRemoteFolder,
    }
}
