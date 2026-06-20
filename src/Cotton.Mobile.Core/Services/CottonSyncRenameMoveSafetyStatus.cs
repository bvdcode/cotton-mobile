namespace Cotton.Mobile.Services
{
    public enum CottonSyncRenameMoveSafetyStatus
    {
        ConflictSafe,
        NeedsFreshFileETag,
        FolderRevisionUnsupported,
        InvalidName,
        DuplicateName,
        InvalidMoveTarget,
        SelfMoveUnsupported,
        NoChange,
    }
}
