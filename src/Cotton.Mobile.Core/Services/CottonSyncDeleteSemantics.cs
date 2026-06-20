namespace Cotton.Mobile.Services
{
    public static class CottonSyncDeleteSemantics
    {
        public static CottonSyncDeleteSemanticsSnapshot Create(
            CottonFileBrowserEntry entry,
            CottonSyncDeleteMode mode)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (!Enum.IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "Delete mode is not supported.");
            }

            if (entry.Type != CottonFileBrowserEntryType.File)
            {
                return new CottonSyncDeleteSemanticsSnapshot(
                    entry.Type,
                    mode,
                    CottonSyncDeleteSafetyStatus.FolderRevisionUnsupported,
                    expectedETag: null,
                    CreateSummary(mode, "Folder revision unavailable"));
            }

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(entry);
            if (!revision.SupportsExpectedETagMutation)
            {
                return new CottonSyncDeleteSemanticsSnapshot(
                    entry.Type,
                    mode,
                    CottonSyncDeleteSafetyStatus.NeedsFreshFileETag,
                    expectedETag: null,
                    CreateSummary(mode, "Fresh file ETag required"));
            }

            return new CottonSyncDeleteSemanticsSnapshot(
                entry.Type,
                mode,
                CottonSyncDeleteSafetyStatus.ConflictSafe,
                revision.ETag,
                CreateSummary(mode, "Expected ETag ready"));
        }

        private static string CreateSummary(CottonSyncDeleteMode mode, string safetyText)
        {
            string modeText = mode == CottonSyncDeleteMode.MoveToTrash
                ? "Move to trash"
                : "Permanent delete";
            return $"{modeText}: {safetyText}";
        }
    }
}
