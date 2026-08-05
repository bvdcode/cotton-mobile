// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUploadReceiptSnapshot
    {
        public CottonUploadReceiptSnapshot(
            string localSourceId,
            string relativePath,
            DateTime localUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            Guid operationId,
            CottonUploadReceiptStatus status,
            DateTime recordedAtUtc,
            Guid? remoteFileId,
            string? remoteETag,
            string? contentHash = null)
        {
            if (string.IsNullOrWhiteSpace(localSourceId))
            {
                throw new ArgumentException("Upload receipt local source id is required.", nameof(localSourceId));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Upload receipt size cannot be negative.");
            }

            if (operationId == Guid.Empty)
            {
                throw new ArgumentException("Upload receipt operation id is required.", nameof(operationId));
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Upload receipt status is not supported.");
            }

            ValidateRemoteRevision(status, remoteFileId, remoteETag);

            LocalSourceId = localSourceId.Trim();
            RelativePath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            LocalUpdatedAtUtc = CottonLocalFileFreshness.NormalizeUtc(localUpdatedAtUtc);
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            OperationId = operationId;
            Status = status;
            RecordedAtUtc = CottonLocalFileFreshness.NormalizeUtc(recordedAtUtc);
            RemoteFileId = remoteFileId;
            RemoteETag = string.IsNullOrWhiteSpace(remoteETag) ? null : remoteETag.Trim();
            ContentHash = CottonContentHash.NormalizeOptionalSha256(contentHash, nameof(contentHash));
        }

        public string LocalSourceId { get; }

        public string RelativePath { get; }

        public DateTime LocalUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public Guid OperationId { get; }

        public CottonUploadReceiptStatus Status { get; }

        public DateTime RecordedAtUtc { get; }

        public Guid? RemoteFileId { get; }

        public string? RemoteETag { get; }

        public string? ContentHash { get; }

        public bool IsPending => Status == CottonUploadReceiptStatus.Pending;

        public bool IsUploaded => Status == CottonUploadReceiptStatus.Uploaded;

        public string OperationMetadataValue => OperationId.ToString("N");

        public static CottonUploadReceiptSnapshot CreatePending(
            CottonDeviceToCloudSyncPlanItem item,
            Guid operationId,
            DateTime recordedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Action != CottonDeviceToCloudSyncActionKind.UploadNewFile
                || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Pending upload receipts require a new-file upload item.", nameof(item));
            }

            string localSourceId = item.LocalSourceId
                ?? throw new ArgumentException("Pending upload receipts require a local source id.", nameof(item));
            DateTime localUpdatedAtUtc = item.LocalUpdatedAtUtc
                ?? throw new ArgumentException("Pending upload receipts require a local update time.", nameof(item));
            string contentHash = item.ContentHash
                ?? throw new ArgumentException("Pending upload receipts require a local content hash.", nameof(item));

            return new CottonUploadReceiptSnapshot(
                localSourceId,
                item.RelativePath,
                localUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                operationId,
                CottonUploadReceiptStatus.Pending,
                recordedAtUtc,
                remoteFileId: null,
                remoteETag: null,
                contentHash);
        }

        public static CottonUploadReceiptSnapshot CreateUploadedFromConfirmation(
            CottonDeviceToCloudSyncPlanItem item,
            DateTime recordedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!item.ConfirmsPendingUpload || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Uploaded receipt confirmation requires a matching file item.", nameof(item));
            }

            string localSourceId = item.LocalSourceId
                ?? throw new ArgumentException("Uploaded receipt confirmation requires a local source id.", nameof(item));
            DateTime localUpdatedAtUtc = item.LocalUpdatedAtUtc
                ?? throw new ArgumentException("Uploaded receipt confirmation requires a local update time.", nameof(item));
            Guid operationId = item.UploadOperationId
                ?? throw new ArgumentException("Uploaded receipt confirmation requires an operation id.", nameof(item));
            Guid remoteFileId = item.CloudItemId
                ?? throw new ArgumentException("Uploaded receipt confirmation requires a remote file id.", nameof(item));
            string remoteETag = item.ExpectedRemoteETag
                ?? throw new ArgumentException("Uploaded receipt confirmation requires a remote ETag.", nameof(item));
            string contentHash = item.ContentHash
                ?? throw new ArgumentException("Uploaded receipt confirmation requires a local content hash.", nameof(item));

            return new CottonUploadReceiptSnapshot(
                localSourceId,
                item.RelativePath,
                localUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                operationId,
                CottonUploadReceiptStatus.Uploaded,
                recordedAtUtc,
                remoteFileId,
                remoteETag,
                contentHash);
        }

        public CottonUploadReceiptSnapshot MarkUploaded(
            CottonFileBrowserEntry remoteFile,
            DateTime recordedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(remoteFile);
            if (!IsPending)
            {
                throw new InvalidOperationException("Only pending upload receipts can be marked uploaded.");
            }

            if (remoteFile.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Uploaded receipt revision requires a remote file.", nameof(remoteFile));
            }

            if (string.IsNullOrWhiteSpace(remoteFile.ETag))
            {
                throw new ArgumentException("Uploaded receipt revision requires a remote ETag.", nameof(remoteFile));
            }

            if (!string.Equals(
                remoteFile.Name,
                CottonSyncRelativePath.GetFileName(RelativePath),
                StringComparison.Ordinal))
            {
                throw new ArgumentException("Uploaded receipt revision returned a different file name.", nameof(remoteFile));
            }

            if (!remoteFile.Metadata.TryGetValue(
                    CottonFileUploadMetadataKeys.UploadOperationId,
                    out string? operationMetadataValue)
                || !string.Equals(operationMetadataValue, OperationMetadataValue, StringComparison.Ordinal))
            {
                throw new ArgumentException("Uploaded receipt revision has a different operation id.", nameof(remoteFile));
            }

            if (SizeBytes.HasValue && remoteFile.SizeBytes != SizeBytes)
            {
                throw new ArgumentException("Uploaded receipt revision has a different file size.", nameof(remoteFile));
            }

            if (ContentHash is null
                || !string.Equals(remoteFile.ContentHash, ContentHash, StringComparison.Ordinal))
            {
                throw new ArgumentException("Uploaded receipt revision has a different content hash.", nameof(remoteFile));
            }

            return new CottonUploadReceiptSnapshot(
                LocalSourceId,
                RelativePath,
                LocalUpdatedAtUtc,
                SizeBytes,
                ContentType,
                OperationId,
                CottonUploadReceiptStatus.Uploaded,
                recordedAtUtc,
                remoteFile.Id,
                remoteFile.ETag,
                ContentHash);
        }

        public bool MatchesLocalSource(CottonDeviceToCloudLocalItemSnapshot localItem)
        {
            ArgumentNullException.ThrowIfNull(localItem);

            return string.Equals(LocalSourceId, localItem.LocalSourceId, StringComparison.Ordinal);
        }

        public bool MatchesLocalVersion(CottonDeviceToCloudLocalItemSnapshot localItem)
        {
            return MatchesLocalSource(localItem)
                && SizeBytes == localItem.SizeBytes
                && LocalUpdatedAtUtc == localItem.LocalUpdatedAtUtc
                && ContentHash is not null
                && localItem.ContentHash is not null
                && string.Equals(ContentHash, localItem.ContentHash, StringComparison.Ordinal);
        }

        private static void ValidateRemoteRevision(
            CottonUploadReceiptStatus status,
            Guid? remoteFileId,
            string? remoteETag)
        {
            switch (status)
            {
                case CottonUploadReceiptStatus.Pending:
                    if (remoteFileId.HasValue || !string.IsNullOrWhiteSpace(remoteETag))
                    {
                        throw new ArgumentException("Pending upload receipts cannot contain a remote revision.");
                    }

                    break;

                case CottonUploadReceiptStatus.Uploaded:
                    if (!remoteFileId.HasValue || remoteFileId.Value == Guid.Empty)
                    {
                        throw new ArgumentException("Uploaded receipts require a remote file id.", nameof(remoteFileId));
                    }

                    if (string.IsNullOrWhiteSpace(remoteETag))
                    {
                        throw new ArgumentException("Uploaded receipts require a remote ETag.", nameof(remoteETag));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), "Upload receipt status is not supported.");
            }
        }
    }
}
