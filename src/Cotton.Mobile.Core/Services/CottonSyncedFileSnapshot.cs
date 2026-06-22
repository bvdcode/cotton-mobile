// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncedFileSnapshot
    {
        public CottonSyncedFileSnapshot(
            Guid fileId,
            string fileName,
            string eTag,
            DateTime remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            DateTime syncedAtUtc,
            string? relativePath = null)
        {
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("Synced file id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Synced file name is required.", nameof(fileName));
            }

            string normalizedName = fileName.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(normalizedName)
                || CottonCloudItemNameRules.ContainsInvalidCharacter(normalizedName))
            {
                throw new ArgumentException("Synced file name must be a direct cloud item name.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(eTag))
            {
                throw new ArgumentException("Synced file ETag is required.", nameof(eTag));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Synced file size cannot be negative.");
            }

            FileId = fileId;
            FileName = normalizedName;
            RelativePath = NormalizeRelativePath(normalizedName, relativePath);
            ETag = eTag.Trim();
            RemoteUpdatedAtUtc = CottonLocalFileFreshness.NormalizeUtc(remoteUpdatedAtUtc);
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            SyncedAtUtc = CottonLocalFileFreshness.NormalizeUtc(syncedAtUtc);
        }

        public Guid FileId { get; }

        public string FileName { get; }

        public string RelativePath { get; }

        public string ETag { get; }

        public DateTime RemoteUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public DateTime SyncedAtUtc { get; }

        public static CottonSyncedFileSnapshot Create(CottonFileBrowserEntry file, DateTime syncedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Synced file metadata requires a file entry.", nameof(file));
            }

            if (string.IsNullOrWhiteSpace(file.ETag))
            {
                throw new ArgumentException("Synced file metadata requires a file ETag.", nameof(file));
            }

            return new CottonSyncedFileSnapshot(
                file.Id,
                file.Name,
                file.ETag,
                file.UpdatedAtUtc,
                file.SizeBytes,
                file.ContentType,
                syncedAtUtc);
        }

        private static string NormalizeRelativePath(string fileName, string? relativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(
                string.IsNullOrWhiteSpace(relativePath) ? fileName : relativePath,
                nameof(relativePath));
            if (!string.Equals(
                CottonSyncRelativePath.GetFileName(normalizedPath),
                fileName,
                StringComparison.Ordinal))
            {
                throw new ArgumentException("Synced file relative path file name must match the file name.", nameof(relativePath));
            }

            return normalizedPath;
        }
    }
}
