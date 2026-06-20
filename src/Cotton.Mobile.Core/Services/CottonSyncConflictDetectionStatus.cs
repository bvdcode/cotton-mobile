namespace Cotton.Mobile.Services
{
    public enum CottonSyncConflictDetectionStatus
    {
        Ready,
        NeedsFreshServerRevision,
        ServerRevisionChanged,
        RemoteTargetMissing,
        RemoteTargetTypeChanged,
        RemoteTargetMismatch,
        FolderRevisionUnsupported,
        RootNotReady,
        WrongSyncRoot,
        TerminalJournalItem,
    }
}
