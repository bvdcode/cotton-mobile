namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudRemoteContentSnapshot
    {
        public CottonDeviceToCloudRemoteContentSnapshot(
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonDeviceToCloudRemoteItemSnapshot> items)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Device-to-cloud remote folder id is required.", nameof(folderId));
            }

            ArgumentNullException.ThrowIfNull(items);

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? "Files" : folderName.Trim();
            Items = items;
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public IReadOnlyList<CottonDeviceToCloudRemoteItemSnapshot> Items { get; }

        public static CottonDeviceToCloudRemoteContentSnapshot FromFolderContent(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            return new CottonDeviceToCloudRemoteContentSnapshot(
                content.FolderId,
                content.FolderName,
                content.Entries
                    .Select(entry => new CottonDeviceToCloudRemoteItemSnapshot(entry, entry.Name))
                    .ToList());
        }
    }
}
