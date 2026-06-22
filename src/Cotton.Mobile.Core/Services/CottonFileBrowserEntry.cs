// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserEntry
    {
        private const string LocalCopyStatusText = "On device";

        private CottonFileBrowserEntry(
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
            CottonOfflineFileAvailabilitySnapshot? offlineAvailability = null,
            CottonLocalFileSnapshot? localFile = null,
            CottonFileThumbnailSnapshot? thumbnail = null,
            bool isSelected = false)
        {
            Id = id;
            Type = type;
            Name = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name.Trim();
            Kind = string.IsNullOrWhiteSpace(kind) ? "File" : kind.Trim();
            Details = details;
            ActionLabel = actionLabel;
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? "FILE" : badgeText.Trim();
            UpdatedAtUtc = updatedAtUtc;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            PreviewHashEncryptedHex = string.IsNullOrWhiteSpace(previewHashEncryptedHex)
                ? null
                : previewHashEncryptedHex.Trim();
            ETag = string.IsNullOrWhiteSpace(eTag) ? null : eTag.Trim();
            OfflineAvailability = offlineAvailability ?? CottonOfflineFileAvailabilitySnapshot.NotPinned;
            LocalFile = localFile;
            Thumbnail = thumbnail ?? CottonFileThumbnailSnapshot.Placeholder(BadgeText, CreateFallbackThumbnailCacheKey());
            IsSelected = isSelected;
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
                    return $"{Details} · {LocalCopyStatusText}";
                }

                return IsOfflineAttentionVisible
                    ? $"{Details} · {OfflineAvailability.StatusText}"
                    : Details;
            }
        }

        public bool HasLocalCopy => LocalFile is not null;

        public string LocalCopyStatus => HasLocalCopy ? LocalCopyStatusText : string.Empty;

        public CottonOfflineFileAvailabilitySnapshot OfflineAvailability { get; }

        public bool IsOfflineAttentionVisible => LocalFile is null && OfflineAvailability.IsAttentionVisible;

        public string OfflineAttentionStatus => IsOfflineAttentionVisible ? OfflineAvailability.StatusText : string.Empty;

        public string ActionLabel { get; }

        public string BadgeText { get; }

        public DateTime UpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public string? PreviewHashEncryptedHex { get; }

        public string? ETag { get; }

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
            ArgumentNullException.ThrowIfNull(node);

            return new CottonFileBrowserEntry(
                node.Id,
                CottonFileBrowserEntryType.Folder,
                node.Name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                node.UpdatedAt,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            ArgumentNullException.ThrowIfNull(file);

            string contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? string.Empty
                : file.ContentType.Trim();
            string kind = CottonFileKindClassifier.ResolveKind(file.Name, contentType);
            return new CottonFileBrowserEntry(
                file.Id,
                CottonFileBrowserEntryType.File,
                file.Name,
                kind,
                $"{CottonFileSizeFormatter.Format(file.SizeBytes)} · {kind}",
                "More",
                ResolveBadgeText(kind),
                file.UpdatedAt,
                file.SizeBytes,
                contentType,
                file.PreviewHashEncryptedHex,
                file.ETag,
                null,
                null);
        }

        public static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag)
        {
            string kind = CottonFileKindClassifier.ResolveKind(name, contentType);
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {kind}"
                : kind;
            return new CottonFileBrowserEntry(
                id,
                CottonFileBrowserEntryType.File,
                name,
                kind,
                details,
                "More",
                ResolveBadgeText(kind),
                updatedAtUtc,
                sizeBytes,
                contentType,
                previewHashEncryptedHex,
                eTag,
                null,
                null);
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
            string? eTag)
        {
            return new CottonFileBrowserEntry(
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
                null,
                null);
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
                PreviewHashEncryptedHex,
                ETag,
                OfflineAvailability,
                LocalFile,
                thumbnail,
                IsSelected);
        }

        public CottonFileBrowserEntry WithLocalFile(CottonLocalFileSnapshot localFile)
        {
            ArgumentNullException.ThrowIfNull(localFile);

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
                PreviewHashEncryptedHex,
                ETag,
                OfflineAvailability,
                localFile,
                Thumbnail,
                IsSelected);
        }

        public CottonFileBrowserEntry WithOfflineAvailability(CottonOfflineFileAvailabilitySnapshot offlineAvailability)
        {
            ArgumentNullException.ThrowIfNull(offlineAvailability);

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
                PreviewHashEncryptedHex,
                ETag,
                offlineAvailability,
                LocalFile,
                Thumbnail,
                IsSelected);
        }

        public CottonFileBrowserEntry WithoutLocalFile()
        {
            if (LocalFile is null)
            {
                return this;
            }

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
                PreviewHashEncryptedHex,
                ETag,
                OfflineAvailability,
                null,
                Thumbnail,
                IsSelected);
        }

        public CottonFileBrowserEntry WithSelection(bool isSelected)
        {
            if (IsSelected == isSelected)
            {
                return this;
            }

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
                PreviewHashEncryptedHex,
                ETag,
                OfflineAvailability,
                LocalFile,
                Thumbnail,
                isSelected);
        }

        private string CreateFallbackThumbnailCacheKey()
        {
            return $"{Type}:{Id:N}:placeholder";
        }

        private static string ResolveBadgeText(string kind)
        {
            return kind switch
            {
                "Image" => "IMG",
                "PDF" => "PDF",
                "Document" => "DOC",
                "Video" => "VID",
                "Audio" => "AUD",
                "SVG" => "SVG",
                "Text" => "TXT",
                _ => "FILE",
            };
        }
    }
}
