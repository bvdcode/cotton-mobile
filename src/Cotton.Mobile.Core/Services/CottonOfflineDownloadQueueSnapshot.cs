namespace Cotton.Mobile.Services
{
    public class CottonOfflineDownloadQueueSnapshot
    {
        public CottonOfflineDownloadQueueSnapshot(
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonOfflineDownloadQueueItem> items)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Folder name is required.", nameof(folderName));
            }

            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0)
            {
                throw new ArgumentException("Offline download queue needs at least one item.", nameof(items));
            }

            FolderId = folderId;
            FolderName = folderName.Trim();
            Items = items;
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public IReadOnlyList<CottonOfflineDownloadQueueItem> Items { get; }

        public int TotalCount => Items.Count;

        public long TotalSizeBytes => Items.Sum(item => item.SizeBytes);

        public static CottonOfflineDownloadQueueSnapshot Create(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(content);
            if (!plan.CanQueueDirectFiles)
            {
                throw new InvalidOperationException("Only ready direct-file offline folder plans can be queued.");
            }

            List<CottonFileBrowserEntry> files = content
                .Entries
                .Where(entry => entry.Type == CottonFileBrowserEntryType.File)
                .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Id)
                .ToList();
            return new CottonOfflineDownloadQueueSnapshot(
                content.FolderId,
                content.FolderName,
                files
                    .Select((file, index) => CottonOfflineDownloadQueueItem.Create(index + 1, file))
                    .ToList());
        }
    }
}
