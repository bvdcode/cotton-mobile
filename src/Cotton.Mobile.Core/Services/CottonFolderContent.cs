namespace Cotton.Mobile.Services
{
    public class CottonFolderContent
    {
        public CottonFolderContent(Guid folderId, string folderName, IReadOnlyList<CottonFileBrowserEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? "Files" : folderName.Trim();
            Entries = entries;
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public IReadOnlyList<CottonFileBrowserEntry> Entries { get; }
    }
}
