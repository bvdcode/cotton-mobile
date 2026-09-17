// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreApproval
    {
        public CottonMediaOriginalRestoreApproval(
            CottonDeviceToCloudLocalItemSnapshot localFile,
            Guid fileId,
            string expectedETag,
            string redactedHash,
            Guid operationId)
        {
            ArgumentNullException.ThrowIfNull(localFile);
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedETag);
            if (fileId == Guid.Empty || operationId == Guid.Empty
                || localFile.ItemType != CottonFileBrowserEntryType.File
                || localFile.ContentHash is null || localFile.LocalSourceId is null || !localFile.SizeBytes.HasValue)
            {
                throw new ArgumentException("Original media approval requires complete file revisions.");
            }

            LocalFile = localFile;
            FileId = fileId;
            ExpectedETag = expectedETag;
            RedactedHash = CottonContentHash.NormalizeOptionalSha256(redactedHash, nameof(redactedHash))
                ?? throw new ArgumentException("A verified redacted hash is required.", nameof(redactedHash));
            OperationId = operationId;
        }

        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; }
        public Guid FileId { get; }
        public string ExpectedETag { get; }
        public string RedactedHash { get; }
        public Guid OperationId { get; }
    }
}
