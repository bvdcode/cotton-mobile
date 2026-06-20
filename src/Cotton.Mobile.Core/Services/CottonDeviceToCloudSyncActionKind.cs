namespace Cotton.Mobile.Services
{
    public enum CottonDeviceToCloudSyncActionKind
    {
        CreateRemoteFolder,
        UploadNewFile,
        UploadChangedFile,
        KeepExistingFile,
        KeepExistingFolder,
        DeleteRemoteFile,
        RemoveManifestOrphan,
        RemotePathConflict,
        RemoteRevisionChanged,
        RemoteTargetMissing,
        NeedsFreshServerRevision,
        BlockedLocalItemName,
    }
}
