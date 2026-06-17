using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserEntry
    {
        private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".css",
            ".csv",
            ".htm",
            ".html",
            ".js",
            ".json",
            ".log",
            ".markdown",
            ".md",
            ".svg",
            ".text",
            ".ts",
            ".txt",
            ".xml",
            ".yaml",
            ".yml",
        };

        private static readonly HashSet<string> TextContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/javascript",
            "application/json",
            "application/markdown",
            "application/xml",
            "application/x-yaml",
            "application/yaml",
            "image/svg+xml",
        };

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
            LocalFile = localFile;
            Thumbnail = thumbnail ?? CottonFileThumbnailSnapshot.Placeholder(BadgeText, CreateFallbackThumbnailCacheKey());
        }

        public Guid Id { get; }

        public CottonFileBrowserEntryType Type { get; }

        public string Name { get; }

        public string Kind { get; }

        public string Details { get; }

        public string DisplayDetails => LocalFile is null ? Details : $"{Details} · On device";

        public string ActionLabel { get; }

        public string ActionButtonText => IsFolder ? ActionLabel : "⋯";

        public double ActionButtonWidth => IsFolder ? 64d : 44d;

        public bool IsTileActionVisible => true;

        public string TileActionButtonText => IsFolder ? "›" : "⋯";

        public double TileActionButtonWidth => 28d;

        public string BadgeText { get; }

        public DateTime UpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public string? PreviewHashEncryptedHex { get; }

        public CottonLocalFileSnapshot? LocalFile { get; }

        public CottonFileThumbnailSnapshot Thumbnail { get; }

        public bool IsFolder => Type == CottonFileBrowserEntryType.Folder;

        public bool IsImage => Type == CottonFileBrowserEntryType.File && Kind == "Image";

        public bool IsText => Type == CottonFileBrowserEntryType.File && Kind == "Text";

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
                "DIR",
                node.UpdatedAt,
                null,
                null,
                null,
                null);
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            ArgumentNullException.ThrowIfNull(file);

            string contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "File"
                : file.ContentType.Trim();
            string kind = ResolveFileKind(file.Name, contentType);
            return new CottonFileBrowserEntry(
                file.Id,
                CottonFileBrowserEntryType.File,
                file.Name,
                kind,
                $"{FormatSize(file.SizeBytes)} · {kind}",
                "More",
                ResolveBadgeText(kind),
                file.UpdatedAt,
                file.SizeBytes,
                contentType,
                file.PreviewHashEncryptedHex,
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
                localFile,
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
                null,
                Thumbnail);
        }

        private static string FormatSize(long bytes)
        {
            const long Kilobyte = 1024;
            const long Megabyte = Kilobyte * 1024;
            const long Gigabyte = Megabyte * 1024;

            return bytes switch
            {
                < Kilobyte => $"{bytes} B",
                < Megabyte => $"{bytes / (double)Kilobyte:0.#} KB",
                < Gigabyte => $"{bytes / (double)Megabyte:0.#} MB",
                _ => $"{bytes / (double)Gigabyte:0.#} GB",
            };
        }

        private string CreateFallbackThumbnailCacheKey()
        {
            return $"{Type}:{Id:N}:placeholder";
        }

        private static string ResolveFileKind(string name, string contentType)
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Image";
            }

            if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "PDF";
            }

            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return "Video";
            }

            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || TextContentTypes.Contains(contentType)
                || TextFileExtensions.Contains(Path.GetExtension(name)))
            {
                return "Text";
            }

            return "File";
        }

        private static string ResolveBadgeText(string kind)
        {
            return kind switch
            {
                "Image" => "IMG",
                "PDF" => "PDF",
                "Video" => "VID",
                "Text" => "TXT",
                _ => "FILE",
            };
        }
    }
}
