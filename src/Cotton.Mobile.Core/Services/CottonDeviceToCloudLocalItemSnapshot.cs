// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudLocalItemSnapshot
    {
        public CottonDeviceToCloudLocalItemSnapshot(
            CottonFileBrowserEntryType itemType,
            string displayName,
            string relativePath,
            DateTime localUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? localSourceId = null)
        {
            if (!Enum.IsDefined(itemType))
            {
                throw new ArgumentOutOfRangeException(nameof(itemType), "Device-to-cloud local item type is not supported.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Device-to-cloud local item name is required.", nameof(displayName));
            }

            string normalizedName = displayName.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(normalizedName)
                || CottonCloudItemNameRules.ContainsInvalidCharacter(normalizedName))
            {
                throw new ArgumentException("Device-to-cloud local item name is invalid.", nameof(displayName));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Device-to-cloud local item size cannot be negative.");
            }

            ItemType = itemType;
            DisplayName = normalizedName;
            RelativePath = NormalizeRelativePath(normalizedName, relativePath);
            LocalUpdatedAtUtc = CottonLocalFileFreshness.NormalizeUtc(localUpdatedAtUtc);
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            LocalSourceId = string.IsNullOrWhiteSpace(localSourceId) ? null : localSourceId.Trim();
        }

        public CottonFileBrowserEntryType ItemType { get; }

        public string DisplayName { get; }

        public string RelativePath { get; }

        public DateTime LocalUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public string? LocalSourceId { get; }

        public static CottonDeviceToCloudLocalItemSnapshot CreateFile(
            string displayName,
            string relativePath,
            DateTime localUpdatedAtUtc,
            long? sizeBytes,
            string? contentType = null,
            string? localSourceId = null)
        {
            return new CottonDeviceToCloudLocalItemSnapshot(
                CottonFileBrowserEntryType.File,
                displayName,
                relativePath,
                localUpdatedAtUtc,
                sizeBytes,
                contentType,
                localSourceId);
        }

        public static CottonDeviceToCloudLocalItemSnapshot CreateFolder(
            string displayName,
            string relativePath,
            DateTime localUpdatedAtUtc,
            string? localSourceId = null)
        {
            return new CottonDeviceToCloudLocalItemSnapshot(
                CottonFileBrowserEntryType.Folder,
                displayName,
                relativePath,
                localUpdatedAtUtc,
                sizeBytes: null,
                contentType: null,
                localSourceId);
        }

        private static string NormalizeRelativePath(string displayName, string relativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            if (!string.Equals(
                CottonSyncRelativePath.GetFileName(normalizedPath),
                displayName,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Device-to-cloud local relative path name must match the item name.",
                    nameof(relativePath));
            }

            return normalizedPath;
        }
    }
}
