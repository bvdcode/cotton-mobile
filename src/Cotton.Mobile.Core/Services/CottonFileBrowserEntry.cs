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
            CottonOfflineFileAvailabilitySnapshot? offlineAvailability = null,
            CottonLocalFileSnapshot? localFile = null,
            CottonFileThumbnailSnapshot? thumbnail = null)
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
            OfflineAvailability = offlineAvailability ?? CottonOfflineFileAvailabilitySnapshot.NotPinned;
            LocalFile = localFile;
            Thumbnail = thumbnail ?? CottonFileThumbnailSnapshot.Placeholder(BadgeText, CreateFallbackThumbnailCacheKey());
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

        public CottonLocalFileSnapshot? LocalFile { get; }

        public CottonFileThumbnailSnapshot Thumbnail { get; }

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
            string? previewHashEncryptedHex)
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
                OfflineAvailability,
                LocalFile,
                thumbnail);
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
                OfflineAvailability,
                localFile,
                Thumbnail);
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
                offlineAvailability,
                LocalFile,
                Thumbnail);
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
                OfflineAvailability,
                null,
                Thumbnail);
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
