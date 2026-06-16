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
            string details,
            string actionLabel)
        {
            Id = id;
            Type = type;
            Name = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name.Trim();
            Details = details;
            ActionLabel = actionLabel;
        }

        public Guid Id { get; }

        public CottonFileBrowserEntryType Type { get; }

        public string Name { get; }

        public string Details { get; }

        public string ActionLabel { get; }

        public bool IsFolder => Type == CottonFileBrowserEntryType.Folder;

        public static CottonFileBrowserEntry FromNode(NodeDto node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return new CottonFileBrowserEntry(
                node.Id,
                CottonFileBrowserEntryType.Folder,
                node.Name,
                "Folder",
                "Open");
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            ArgumentNullException.ThrowIfNull(file);

            string contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "File"
                : file.ContentType.Trim();
            return new CottonFileBrowserEntry(
                file.Id,
                CottonFileBrowserEntryType.File,
                file.Name,
                $"{FormatSize(file.SizeBytes)} · {contentType}",
                "Download");
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
    }
}
