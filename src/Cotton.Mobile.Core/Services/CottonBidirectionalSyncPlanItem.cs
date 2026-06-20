namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncPlanItem
    {
        public CottonBidirectionalSyncPlanItem(
            CottonBidirectionalSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath,
            string? previousRelativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            DateTime? localUpdatedAtUtc,
            DateTime? remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? localSourceId = null)
        {
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Bidirectional sync action is not supported.");
            }

            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Bidirectional sync target type is not supported.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Bidirectional sync display name is required.", nameof(displayName));
            }

            if (cloudItemId == Guid.Empty)
            {
                throw new ArgumentException("Bidirectional sync cloud item id cannot be empty.", nameof(cloudItemId));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Bidirectional sync item size cannot be negative.");
            }

            Action = action;
            TargetType = targetType;
            DisplayName = displayName.Trim();
            RelativePath = NormalizeRelativePath(Action, TargetType, DisplayName, relativePath);
            PreviousRelativePath = NormalizePreviousRelativePath(previousRelativePath);
            CloudItemId = cloudItemId;
            ExpectedRemoteETag = string.IsNullOrWhiteSpace(expectedRemoteETag) ? null : expectedRemoteETag.Trim();
            LocalUpdatedAtUtc = localUpdatedAtUtc.HasValue
                ? CottonLocalFileFreshness.NormalizeUtc(localUpdatedAtUtc.Value)
                : null;
            RemoteUpdatedAtUtc = remoteUpdatedAtUtc.HasValue
                ? CottonLocalFileFreshness.NormalizeUtc(remoteUpdatedAtUtc.Value)
                : null;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            LocalSourceId = string.IsNullOrWhiteSpace(localSourceId) ? null : localSourceId.Trim();
        }

        public CottonBidirectionalSyncActionKind Action { get; }

        public CottonFileBrowserEntryType TargetType { get; }

        public string DisplayName { get; }

        public string RelativePath { get; }

        public string? PreviousRelativePath { get; }

        public Guid? CloudItemId { get; }

        public string? ExpectedRemoteETag { get; }

        public DateTime? LocalUpdatedAtUtc { get; }

        public DateTime? RemoteUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public string? LocalSourceId { get; }

        public bool RequiresDownload =>
            Action is CottonBidirectionalSyncActionKind.DownloadNewFile
                or CottonBidirectionalSyncActionKind.RefreshLocalFile;

        public bool RequiresLocalRename => Action == CottonBidirectionalSyncActionKind.RenameLocalFile;

        public bool RequiresLocalDelete => Action == CottonBidirectionalSyncActionKind.RemoveLocalFile;

        public bool RequiresUpload =>
            Action is CottonBidirectionalSyncActionKind.UploadNewFile
                or CottonBidirectionalSyncActionKind.UploadChangedFile;

        public bool RequiresRemoteFolderCreate => Action == CottonBidirectionalSyncActionKind.CreateRemoteFolder;

        public bool RequiresRemoteDelete => Action == CottonBidirectionalSyncActionKind.DeleteRemoteFile;

        public bool RemovesManifestOnly => Action == CottonBidirectionalSyncActionKind.RemoveManifestOrphan;

        public bool RequiresCloudToDeviceMutation => RequiresDownload || RequiresLocalRename || RequiresLocalDelete;

        public bool RequiresDeviceToCloudMutation =>
            RequiresUpload || RequiresRemoteFolderCreate || RequiresRemoteDelete;

        public bool IsDestructive => RequiresLocalDelete || RequiresRemoteDelete;

        public bool IsNoOp =>
            Action is CottonBidirectionalSyncActionKind.KeepExistingFile
                or CottonBidirectionalSyncActionKind.KeepExistingFolder;

        public bool IsBlocked =>
            Action is CottonBidirectionalSyncActionKind.FileChangedOnBothSides
                or CottonBidirectionalSyncActionKind.RemotePathConflict
                or CottonBidirectionalSyncActionKind.RemoteTargetMissing
                or CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                or CottonBidirectionalSyncActionKind.BlockedLocalItemName
                or CottonBidirectionalSyncActionKind.BlockedRemoteFolder;

        public bool IsConflict =>
            Action is CottonBidirectionalSyncActionKind.FileChangedOnBothSides
                or CottonBidirectionalSyncActionKind.RemotePathConflict
                or CottonBidirectionalSyncActionKind.RemoteTargetMissing;

        private static string NormalizeRelativePath(
            CottonBidirectionalSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath)
        {
            if (action == CottonBidirectionalSyncActionKind.BlockedLocalItemName)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new ArgumentException("Bidirectional sync relative path is required.", nameof(relativePath));
                }

                return relativePath.Trim();
            }

            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            if (targetType == CottonFileBrowserEntryType.File
                && !string.Equals(
                    CottonSyncRelativePath.GetFileName(normalizedPath),
                    displayName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Bidirectional sync relative path file name must match the display name.",
                    nameof(relativePath));
            }

            return normalizedPath;
        }

        private static string? NormalizePreviousRelativePath(string? previousRelativePath)
        {
            return string.IsNullOrWhiteSpace(previousRelativePath)
                ? null
                : CottonSyncRelativePath.NormalizeFilePath(previousRelativePath, nameof(previousRelativePath));
        }
    }
}
