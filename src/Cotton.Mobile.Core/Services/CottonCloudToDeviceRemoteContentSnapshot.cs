namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceRemoteContentSnapshot
    {
        public CottonCloudToDeviceRemoteContentSnapshot(
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonCloudToDeviceRemoteItemSnapshot> entries)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Remote sync folder id is required.", nameof(folderId));
            }

            ArgumentNullException.ThrowIfNull(entries);

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? "Files" : folderName.Trim();
            Entries = entries;
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public IReadOnlyList<CottonCloudToDeviceRemoteItemSnapshot> Entries { get; }

        public static CottonCloudToDeviceRemoteContentSnapshot FromFolderContent(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            return new CottonCloudToDeviceRemoteContentSnapshot(
                content.FolderId,
                content.FolderName,
                content.Entries
                    .Select(entry => new CottonCloudToDeviceRemoteItemSnapshot(entry, entry.Name))
                    .ToList());
        }
    }
}
