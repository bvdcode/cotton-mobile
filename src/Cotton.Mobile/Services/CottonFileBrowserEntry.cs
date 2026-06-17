using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserEntry
    {
        private CottonFileBrowserEntry(
            Guid id,
            CottonFileBrowserEntryType type,
            string name,
            string kind,
            string details,
            string actionLabel,
            string badgeText,
            long? sizeBytes,
            string? contentType)
        {
            Id = id;
            Type = type;
            Name = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name.Trim();
            Kind = string.IsNullOrWhiteSpace(kind) ? "File" : kind.Trim();
            Details = details;
            ActionLabel = actionLabel;
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? "FILE" : badgeText.Trim();
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public Guid Id { get; }

        public CottonFileBrowserEntryType Type { get; }

        public string Name { get; }

        public string Kind { get; }

        public string Details { get; }

        public string ActionLabel { get; }

        public string BadgeText { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public bool IsFolder => Type == CottonFileBrowserEntryType.Folder;

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
                "Download",
                ResolveBadgeText(kind),
                file.SizeBytes,
                contentType);
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

            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
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
