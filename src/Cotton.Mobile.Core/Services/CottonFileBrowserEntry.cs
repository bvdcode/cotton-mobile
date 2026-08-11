// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Nodes;
using System.Collections.ObjectModel;
using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserEntry
    {
        internal CottonFileBrowserEntry(
            Guid id,
            CottonFileBrowserEntryType type,
            string name,
            string kind,
            string details,
            string actionLabel,
            string badgeText,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? contentHash,
            string? previewHashEncryptedHex,
            string? eTag,
            CottonOfflineFileAvailabilitySnapshot? offlineAvailability = null,
            CottonLocalFileSnapshot? localFile = null,
            CottonFileThumbnailSnapshot? thumbnail = null,
            bool isSelected = false,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            Id = id;
            Type = type;
            Name = string.IsNullOrWhiteSpace(name) ? CoreResources.UnnamedItem : name.Trim();
            Kind = string.IsNullOrWhiteSpace(kind) ? "File" : kind.Trim();
            Details = details;
            ActionLabel = actionLabel;
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? CoreResources.FileBadge : badgeText.Trim();
            UpdatedAtUtc = updatedAtUtc;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            ContentHash = CottonContentHash.NormalizeOptionalSha256(contentHash, nameof(contentHash));
            PreviewHashEncryptedHex = string.IsNullOrWhiteSpace(previewHashEncryptedHex)
                ? null
                : previewHashEncryptedHex.Trim();
            ETag = string.IsNullOrWhiteSpace(eTag) ? null : eTag.Trim();
            OfflineAvailability = offlineAvailability ?? CottonOfflineFileAvailabilitySnapshot.NotPinned;
            LocalFile = localFile;
            Thumbnail = thumbnail ?? CottonFileThumbnailSnapshot.Placeholder(BadgeText, CreateFallbackThumbnailCacheKey());
            IsSelected = isSelected;
            Metadata = CreateMetadata(metadata);
        }

        public Guid Id { get; }

        public CottonFileBrowserEntryType Type { get; }

        public string Name { get; }

        public string Kind { get; }

        public string Details { get; }

        public string DisplayDetails
        {
            get
            {
                if (LocalFile is not null)
                {
                    return $"{Details} · {CoreResources.OnDeviceStatus}";
                }

                return IsOfflineAttentionVisible
                    ? $"{Details} · {OfflineAvailability.StatusText}"
                    : Details;
            }
        }

        public bool HasLocalCopy => LocalFile is not null;

        public string LocalCopyStatus => HasLocalCopy ? CoreResources.OnDeviceStatus : string.Empty;

        public CottonOfflineFileAvailabilitySnapshot OfflineAvailability { get; }

        public bool IsOfflineAttentionVisible => LocalFile is null && OfflineAvailability.IsAttentionVisible;

        public string OfflineAttentionStatus => IsOfflineAttentionVisible ? OfflineAvailability.StatusText : string.Empty;

        public string ActionLabel { get; }

        public string BadgeText { get; }

        public DateTime UpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public string? ContentHash { get; }

        public string? PreviewHashEncryptedHex { get; }

        public string? ETag { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public CottonLocalFileSnapshot? LocalFile { get; }

        public CottonFileThumbnailSnapshot Thumbnail { get; }

        public bool IsSelected { get; }

        public bool IsFolder => Type == CottonFileBrowserEntryType.Folder;

        public bool IsFolderThumbnailVisible => IsFolder && Thumbnail.IsPlaceholderVisible;

        public bool IsPlaceholderTextVisible => !IsFolder && (Thumbnail.IsPlaceholderVisible || (IsText && Thumbnail.HasImage));

        public bool IsPreviewImageVisible => Thumbnail.HasImage && !IsText;

        public bool IsImage => Type == CottonFileBrowserEntryType.File && Kind == "Image";

        public bool IsText => Type == CottonFileBrowserEntryType.File && Kind == "Text";

        public bool IsSvg => Type == CottonFileBrowserEntryType.File && Kind == "SVG";

        public static CottonFileBrowserEntry FromNode(NodeDto node)
        {
            return CottonFileBrowserEntryFactory.FromNode(node);
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            return CottonFileBrowserEntryFactory.FromFile(file);
        }

        public static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag,
            IReadOnlyDictionary<string, string>? metadata = null,
            string? contentHash = null)
        {
            return CottonFileBrowserEntryFactory.CreateFile(
                id,
                name,
                updatedAtUtc,
                sizeBytes,
                contentType,
                previewHashEncryptedHex,
                eTag,
                metadata,
                contentHash);
        }

        public static CottonFileBrowserEntry CreateCached(
            Guid id,
            CottonFileBrowserEntryType type,
            string name,
            string kind,
            string details,
            string actionLabel,
            string badgeText,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag,
            string? contentHash = null)
        {
            return CottonFileBrowserEntryFactory.CreateCached(
                id,
                type,
                name,
                kind,
                details,
                actionLabel,
                badgeText,
                updatedAtUtc,
                sizeBytes,
                contentType,
                previewHashEncryptedHex,
                eTag,
                contentHash);
        }

        public bool Matches(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string query = searchText.Trim();
            return Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Kind.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Details.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (ContentType?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public CottonFileBrowserEntry WithThumbnail(CottonFileThumbnailSnapshot thumbnail)
        {
            ArgumentNullException.ThrowIfNull(thumbnail);

            return Copy(OfflineAvailability, LocalFile, thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithLocalFile(CottonLocalFileSnapshot localFile)
        {
            ArgumentNullException.ThrowIfNull(localFile);

            return Copy(OfflineAvailability, localFile, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithOfflineAvailability(CottonOfflineFileAvailabilitySnapshot offlineAvailability)
        {
            ArgumentNullException.ThrowIfNull(offlineAvailability);

            return Copy(offlineAvailability, LocalFile, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithoutLocalFile()
        {
            if (LocalFile is null)
            {
                return this;
            }

            return Copy(OfflineAvailability, null, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithSelection(bool isSelected)
        {
            if (IsSelected == isSelected)
            {
                return this;
            }

            return Copy(OfflineAvailability, LocalFile, Thumbnail, isSelected);
        }

        private CottonFileBrowserEntry Copy(
            CottonOfflineFileAvailabilitySnapshot offlineAvailability,
            CottonLocalFileSnapshot? localFile,
            CottonFileThumbnailSnapshot thumbnail,
            bool isSelected)
        {
            return new CottonFileBrowserEntry(
                Id,
                Type,
                Name,
                Kind,
                Details,
                ActionLabel,
                BadgeText,
                UpdatedAtUtc,
                SizeBytes,
                ContentType,
                ContentHash,
                PreviewHashEncryptedHex,
                ETag,
                offlineAvailability,
                localFile,
                thumbnail,
                isSelected,
                Metadata);
        }

        private string CreateFallbackThumbnailCacheKey()
        {
            return $"{Type}:{Id:N}:placeholder";
        }

        private static IReadOnlyDictionary<string, string> CreateMetadata(
            IReadOnlyDictionary<string, string>? metadata)
        {
            Dictionary<string, string> values = metadata is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
            return new ReadOnlyDictionary<string, string>(values);
        }
    }
}
