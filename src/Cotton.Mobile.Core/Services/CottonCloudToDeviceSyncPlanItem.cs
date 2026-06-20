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
            long? sizeBytes)
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
            RemoteETag = string.IsNullOrWhiteSpace(remoteETag) ? null : remoteETag.Trim();
            SizeBytes = sizeBytes;
        }

        public CottonCloudToDeviceSyncActionKind Action { get; }

        public CottonFileBrowserEntryType TargetType { get; }

        public Guid TargetId { get; }

        public string DisplayName { get; }

        public string? RemoteETag { get; }

        public long? SizeBytes { get; }

        public bool RequiresDownload =>
            Action is CottonCloudToDeviceSyncActionKind.DownloadNewFile
                or CottonCloudToDeviceSyncActionKind.RefreshChangedFile;

        public bool RequiresLocalRename => Action == CottonCloudToDeviceSyncActionKind.RenameLocalFile;

        public bool RemovesLocalFile => Action == CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan;

        public bool IsBlocked =>
            Action is CottonCloudToDeviceSyncActionKind.BlockedFolder
                or CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision;

        public bool IsNoOp => Action == CottonCloudToDeviceSyncActionKind.KeepExistingFile;
    }
}
