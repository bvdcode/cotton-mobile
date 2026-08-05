// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
            string? contentType,
            string? localSourceId = null,
            Guid? uploadOperationId = null,
            string? contentHash = null)
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

            if (uploadOperationId == Guid.Empty)
            {
                throw new ArgumentException("Upload operation id cannot be empty.", nameof(uploadOperationId));
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
            LocalSourceId = string.IsNullOrWhiteSpace(localSourceId) ? null : localSourceId.Trim();
            UploadOperationId = uploadOperationId;
            ContentHash = CottonContentHash.NormalizeOptionalSha256(contentHash, nameof(contentHash));
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

        public string? LocalSourceId { get; }

        public Guid? UploadOperationId { get; }

        public string? ContentHash { get; }

        public bool RequiresUpload =>
            Action is CottonDeviceToCloudSyncActionKind.UploadNewFile
                or CottonDeviceToCloudSyncActionKind.UploadChangedFile;

        public bool RequiresRemoteFolderCreate => Action == CottonDeviceToCloudSyncActionKind.CreateRemoteFolder;

        public bool ConfirmsPendingUpload => Action == CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload;

        public bool RequiresLocalDelete => Action == CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile;

        public bool RequiresRemoteDelete => Action == CottonDeviceToCloudSyncActionKind.DeleteRemoteFile;

        public bool RemovesManifestOnly => Action == CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan;

        public bool RequiresServerMutation => RequiresUpload || RequiresRemoteFolderCreate || RequiresRemoteDelete;

        public bool RequiresLocalMutation => ConfirmsPendingUpload || RequiresLocalDelete;

        public bool IsNoOp =>
            Action is CottonDeviceToCloudSyncActionKind.KeepExistingFile
                or CottonDeviceToCloudSyncActionKind.KeepExistingFolder;

        public bool IsBlocked =>
            Action is CottonDeviceToCloudSyncActionKind.RemotePathConflict
                or CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision
                or CottonDeviceToCloudSyncActionKind.BlockedLocalItemName
                or CottonDeviceToCloudSyncActionKind.BlockedLocalSource
                or CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged;

        public bool IsLocalProblem => Action == CottonDeviceToCloudSyncActionKind.BlockedLocalItemName;

        public CottonSyncedFileSnapshot CreateManifestItem(
            CottonFileBrowserEntry uploadedFile,
            DateTime syncedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(uploadedFile);
            if (!RequiresUpload)
            {
                throw new InvalidOperationException("Only upload sync plan items can create synced-file metadata.");
            }

            if (uploadedFile.Type != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Synced-file metadata requires an uploaded file item.");
            }

            if (!string.Equals(uploadedFile.Name, DisplayName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Synced-file metadata requires the uploaded file name to match the plan item.");
            }

            if (Action == CottonDeviceToCloudSyncActionKind.UploadChangedFile
                && CloudItemId.HasValue
                && uploadedFile.Id != CloudItemId.Value)
            {
                throw new InvalidOperationException("Changed upload returned a different cloud file id.");
            }

            if (string.IsNullOrWhiteSpace(uploadedFile.ETag))
            {
                throw new InvalidOperationException("Synced-file metadata requires an uploaded file ETag.");
            }

            string uploadedContentHash = uploadedFile.ContentHash
                ?? throw new InvalidOperationException("Synced-file metadata requires an uploaded file content hash.");
            if (ContentHash is null || !string.Equals(uploadedContentHash, ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Uploaded file content hash does not match the local file.");
            }

            return new CottonSyncedFileSnapshot(
                uploadedFile.Id,
                uploadedFile.Name,
                uploadedFile.ETag,
                uploadedFile.UpdatedAtUtc,
                uploadedFile.SizeBytes,
                uploadedFile.ContentType,
                syncedAtUtc,
                RelativePath,
                uploadedContentHash);
        }

        public CottonDeviceToCloudSyncPlanItem WithUploadOperationId(Guid uploadOperationId)
        {
            if (uploadOperationId == Guid.Empty)
            {
                throw new ArgumentException("Upload operation id is required.", nameof(uploadOperationId));
            }

            return new CottonDeviceToCloudSyncPlanItem(
                Action,
                TargetType,
                DisplayName,
                RelativePath,
                CloudItemId,
                ExpectedRemoteETag,
                LocalUpdatedAtUtc,
                SizeBytes,
                ContentType,
                LocalSourceId,
                uploadOperationId,
                ContentHash);
        }

        private static string NormalizeRelativePath(
            CottonDeviceToCloudSyncActionKind action,
            string displayName,
            string relativePath)
        {
            if (action is CottonDeviceToCloudSyncActionKind.BlockedLocalItemName
                or CottonDeviceToCloudSyncActionKind.BlockedLocalSource)
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
