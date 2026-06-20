namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncPlanItem
    {
        public CottonCloudToDeviceSyncPlanItem(
            CottonCloudToDeviceSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            Guid targetId,
            string displayName,
            string? remoteETag,
            DateTime? remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? relativePath = null,
            string? previousRelativePath = null)
        {
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Cloud-to-device sync action is not supported.");
            }

            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Cloud-to-device sync target type is not supported.");
            }

            if (targetId == Guid.Empty)
            {
                throw new ArgumentException("Cloud-to-device sync target id is required.", nameof(targetId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Cloud-to-device sync display name is required.", nameof(displayName));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Cloud-to-device sync item size cannot be negative.");
            }

            Action = action;
            TargetType = targetType;
            TargetId = targetId;
            DisplayName = displayName.Trim();
            RelativePath = NormalizeRelativePath(TargetType, DisplayName, relativePath);
            PreviousRelativePath = NormalizePreviousRelativePath(previousRelativePath);
            RemoteETag = string.IsNullOrWhiteSpace(remoteETag) ? null : remoteETag.Trim();
            RemoteUpdatedAtUtc = remoteUpdatedAtUtc.HasValue
                ? CottonLocalFileFreshness.NormalizeUtc(remoteUpdatedAtUtc.Value)
                : null;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public CottonCloudToDeviceSyncActionKind Action { get; }

        public CottonFileBrowserEntryType TargetType { get; }

        public Guid TargetId { get; }

        public string DisplayName { get; }

        public string RelativePath { get; }

        public string? PreviousRelativePath { get; }

        public string? RemoteETag { get; }

        public DateTime? RemoteUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public bool RequiresDownload =>
            Action is CottonCloudToDeviceSyncActionKind.DownloadNewFile
                or CottonCloudToDeviceSyncActionKind.RefreshChangedFile;

        public bool RequiresLocalRename => Action == CottonCloudToDeviceSyncActionKind.RenameLocalFile;

        public bool RemovesLocalFile => Action == CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan;

        public bool IsBlocked =>
            Action is CottonCloudToDeviceSyncActionKind.BlockedFolder
                or CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision;

        public bool IsNoOp => Action == CottonCloudToDeviceSyncActionKind.KeepExistingFile;

        public bool ChangesRelativePath =>
            RequiresLocalRename
            && !string.IsNullOrWhiteSpace(PreviousRelativePath)
            && !string.Equals(PreviousRelativePath, RelativePath, StringComparison.Ordinal);

        public CottonSyncedFileSnapshot CreateManifestItem(DateTime syncedAtUtc)
        {
            if (TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only file sync plan items can create synced-file metadata.");
            }

            if (string.IsNullOrWhiteSpace(RemoteETag) || !RemoteUpdatedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Synced-file metadata requires a remote ETag and updated timestamp.");
            }

            return new CottonSyncedFileSnapshot(
                TargetId,
                DisplayName,
                RemoteETag,
                RemoteUpdatedAtUtc.Value,
                SizeBytes,
                ContentType,
                syncedAtUtc,
                RelativePath);
        }

        private static string NormalizeRelativePath(
            CottonFileBrowserEntryType targetType,
            string displayName,
            string? relativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(
                string.IsNullOrWhiteSpace(relativePath) ? displayName : relativePath,
                nameof(relativePath));
            if (targetType == CottonFileBrowserEntryType.File
                && !string.Equals(
                    CottonSyncRelativePath.GetFileName(normalizedPath),
                    displayName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Cloud-to-device sync relative path file name must match the display name.",
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
