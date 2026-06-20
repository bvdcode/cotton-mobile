namespace Cotton.Mobile.Services
{
    public class CottonSyncRenameMoveSemanticsSnapshot
    {
        public CottonSyncRenameMoveSemanticsSnapshot(
            CottonFileBrowserEntryType targetType,
            CottonSyncRenameMoveOperation operation,
            CottonSyncRenameMoveSafetyStatus safetyStatus,
            string? expectedETag,
            string? normalizedName,
            Guid? targetParentId,
            string summaryText)
        {
            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Rename/move target type is not supported.");
            }

            if (!Enum.IsDefined(operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), "Rename/move operation is not supported.");
            }

            if (!Enum.IsDefined(safetyStatus))
            {
                throw new ArgumentOutOfRangeException(nameof(safetyStatus), "Rename/move safety status is not supported.");
            }

            TargetType = targetType;
            Operation = operation;
            SafetyStatus = safetyStatus;
            ExpectedETag = string.IsNullOrWhiteSpace(expectedETag) ? null : expectedETag.Trim();
            NormalizedName = string.IsNullOrWhiteSpace(normalizedName) ? null : normalizedName.Trim();
            TargetParentId = targetParentId == Guid.Empty ? null : targetParentId;
            SummaryText = string.IsNullOrWhiteSpace(summaryText)
                ? throw new ArgumentException("Summary text is required.", nameof(summaryText))
                : summaryText;
        }

        public CottonFileBrowserEntryType TargetType { get; }

        public CottonSyncRenameMoveOperation Operation { get; }

        public CottonSyncRenameMoveSafetyStatus SafetyStatus { get; }

        public string? ExpectedETag { get; }

        public string? NormalizedName { get; }

        public Guid? TargetParentId { get; }

        public string SummaryText { get; }

        public bool HasConflictPrecondition => SafetyStatus == CottonSyncRenameMoveSafetyStatus.ConflictSafe;

        public bool RequiresExpectedETag => TargetType == CottonFileBrowserEntryType.File
            && SafetyStatus is CottonSyncRenameMoveSafetyStatus.ConflictSafe
                or CottonSyncRenameMoveSafetyStatus.NeedsFreshFileETag;

        public bool RequiresFreshListing => SafetyStatus == CottonSyncRenameMoveSafetyStatus.NeedsFreshFileETag;

        public bool HasFolderRevisionGap => SafetyStatus == CottonSyncRenameMoveSafetyStatus.FolderRevisionUnsupported;

        public bool IsRejected => SafetyStatus is CottonSyncRenameMoveSafetyStatus.InvalidName
            or CottonSyncRenameMoveSafetyStatus.DuplicateName
            or CottonSyncRenameMoveSafetyStatus.InvalidMoveTarget
            or CottonSyncRenameMoveSafetyStatus.SelfMoveUnsupported;

        public bool IsNoChange => SafetyStatus == CottonSyncRenameMoveSafetyStatus.NoChange;
    }
}
