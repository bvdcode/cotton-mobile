namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncPlanItem
    {
        public CottonDeviceToCloudSyncPlanItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            DateTime? localUpdatedAtUtc,
            long? sizeBytes,
            string? contentType)
        {
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Device-to-cloud sync action is not supported.");
            }

            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Device-to-cloud sync target type is not supported.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Device-to-cloud sync display name is required.", nameof(displayName));
            }

            if (cloudItemId == Guid.Empty)
            {
                throw new ArgumentException("Device-to-cloud cloud item id cannot be empty.", nameof(cloudItemId));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Device-to-cloud sync item size cannot be negative.");
            }

            Action = action;
            TargetType = targetType;
            DisplayName = displayName.Trim();
            RelativePath = NormalizeRelativePath(Action, DisplayName, relativePath);
            CloudItemId = cloudItemId;
            ExpectedRemoteETag = string.IsNullOrWhiteSpace(expectedRemoteETag) ? null : expectedRemoteETag.Trim();
            LocalUpdatedAtUtc = localUpdatedAtUtc.HasValue
                ? CottonLocalFileFreshness.NormalizeUtc(localUpdatedAtUtc.Value)
                : null;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public CottonDeviceToCloudSyncActionKind Action { get; }

        public CottonFileBrowserEntryType TargetType { get; }

        public string DisplayName { get; }

        public string RelativePath { get; }

        public Guid? CloudItemId { get; }

        public string? ExpectedRemoteETag { get; }

        public DateTime? LocalUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public bool RequiresUpload =>
            Action is CottonDeviceToCloudSyncActionKind.UploadNewFile
                or CottonDeviceToCloudSyncActionKind.UploadChangedFile;

        public bool RequiresRemoteFolderCreate => Action == CottonDeviceToCloudSyncActionKind.CreateRemoteFolder;

        public bool RequiresRemoteDelete => Action == CottonDeviceToCloudSyncActionKind.DeleteRemoteFile;

        public bool RemovesManifestOnly => Action == CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan;

        public bool RequiresServerMutation => RequiresUpload || RequiresRemoteFolderCreate || RequiresRemoteDelete;

        public bool IsDestructive => RequiresRemoteDelete;

        public bool IsNoOp =>
            Action is CottonDeviceToCloudSyncActionKind.KeepExistingFile
                or CottonDeviceToCloudSyncActionKind.KeepExistingFolder;

        public bool IsBlocked =>
            Action is CottonDeviceToCloudSyncActionKind.RemotePathConflict
                or CottonDeviceToCloudSyncActionKind.RemoteRevisionChanged
                or CottonDeviceToCloudSyncActionKind.RemoteTargetMissing
                or CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision
                or CottonDeviceToCloudSyncActionKind.BlockedLocalItemName;

        public bool IsLocalProblem => Action == CottonDeviceToCloudSyncActionKind.BlockedLocalItemName;

        private static string NormalizeRelativePath(
            CottonDeviceToCloudSyncActionKind action,
            string displayName,
            string relativePath)
        {
            if (action == CottonDeviceToCloudSyncActionKind.BlockedLocalItemName)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new ArgumentException("Device-to-cloud sync relative path is required.", nameof(relativePath));
                }

                return relativePath.Trim();
            }

            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            if (!string.Equals(
                CottonSyncRelativePath.GetFileName(normalizedPath),
                displayName,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Device-to-cloud sync relative path name must match the display name.",
                    nameof(relativePath));
            }

            return normalizedPath;
        }
    }
}
