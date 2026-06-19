namespace Cotton.Mobile.Services
{
    public class CottonOfflineFolderPlanSnapshot
    {
        public CottonOfflineFolderPlanSnapshot(
            Guid folderId,
            string folderName,
            int fileCount,
            int folderCount,
            long knownSizeBytes,
            int unknownSizeFileCount)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Folder name is required.", nameof(folderName));
            }

            if (fileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "File count cannot be negative.");
            }

            if (folderCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(folderCount), "Folder count cannot be negative.");
            }

            if (knownSizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(knownSizeBytes), "Size cannot be negative.");
            }

            if (unknownSizeFileCount < 0 || unknownSizeFileCount > fileCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unknownSizeFileCount),
                    "Unknown-size file count must fit within the file count.");
            }

            FolderId = folderId;
            FolderName = folderName.Trim();
            FileCount = fileCount;
            FolderCount = folderCount;
            KnownSizeBytes = knownSizeBytes;
            UnknownSizeFileCount = unknownSizeFileCount;
            Status = ResolveStatus(fileCount, folderCount, unknownSizeFileCount);
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public int FileCount { get; }

        public int FolderCount { get; }

        public long KnownSizeBytes { get; }

        public int UnknownSizeFileCount { get; }

        public CottonOfflineFolderPlanStatus Status { get; }

        public bool HasExactSize => UnknownSizeFileCount == 0;

        public bool CanQueueDirectFiles => Status == CottonOfflineFolderPlanStatus.Ready;

        public static CottonOfflineFolderPlanSnapshot Create(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            List<CottonFileBrowserEntry> files = content
                .Entries
                .Where(entry => entry.Type == CottonFileBrowserEntryType.File)
                .ToList();
            int folderCount = content.Entries.Count(entry => entry.Type == CottonFileBrowserEntryType.Folder);
            long knownSizeBytes = files
                .Where(file => file.SizeBytes.HasValue)
                .Sum(file => file.SizeBytes!.Value);
            int unknownSizeFileCount = files.Count(file => !file.SizeBytes.HasValue);

            return new CottonOfflineFolderPlanSnapshot(
                content.FolderId,
                content.FolderName,
                files.Count,
                folderCount,
                knownSizeBytes,
                unknownSizeFileCount);
        }

        private static CottonOfflineFolderPlanStatus ResolveStatus(
            int fileCount,
            int folderCount,
            int unknownSizeFileCount)
        {
            if (fileCount == 0 && folderCount == 0)
            {
                return CottonOfflineFolderPlanStatus.Empty;
            }

            if (folderCount > 0)
            {
                return CottonOfflineFolderPlanStatus.ContainsFolders;
            }

            return unknownSizeFileCount > 0
                ? CottonOfflineFolderPlanStatus.HasUnknownSize
                : CottonOfflineFolderPlanStatus.Ready;
        }
    }
}
