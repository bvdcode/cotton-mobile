namespace Cotton.Mobile.Services
{
    public class CottonCachedFolderContent
    {
        public int SchemaVersion { get; set; }

        public Guid FolderId { get; set; }

        public string FolderName { get; set; } = string.Empty;

        public DateTime CachedAtUtc { get; set; }

        public List<CottonCachedFileBrowserEntry> Entries { get; set; } = [];
    }
}
