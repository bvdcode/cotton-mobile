namespace Cotton.Mobile.Services
{
    public class CottonShareDestinationSnapshot
    {
        public CottonShareDestinationSnapshot(Guid folderId, string folderName, string? path)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Share destination folder id cannot be empty.", nameof(folderId));
            }

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? "Files" : folderName.Trim();
            Path = string.IsNullOrWhiteSpace(path) ? FolderName : path.Trim();
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public string Path { get; }
    }
}
