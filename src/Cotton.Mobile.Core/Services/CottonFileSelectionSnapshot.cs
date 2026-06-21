namespace Cotton.Mobile.Services
{
    public class CottonFileSelectionSnapshot
    {
        private const string SelectFilesOnlyReason = "Select only files for this action.";
        private const string LocalFilesOnlyReason = "Download files before sharing them.";
        private const string OfflineFilesOnlyReason = "No selected files are stored on this device.";

        private CottonFileSelectionSnapshot(IReadOnlyList<CottonFileBrowserEntry> entries)
        {
            Entries = entries;
            Count = entries.Count;
            FileCount = entries.Count(entry => entry.Type == CottonFileBrowserEntryType.File);
            FolderCount = entries.Count(entry => entry.Type == CottonFileBrowserEntryType.Folder);
            LocalFileCount = entries.Count(entry => entry.Type == CottonFileBrowserEntryType.File && entry.HasLocalCopy);
            OfflineAttentionCount = entries.Count(
                entry => entry.Type == CottonFileBrowserEntryType.File && entry.IsOfflineAttentionVisible);
            TitleText = Count == 1 ? "1 selected" : $"{Count} selected";
            DetailText = CreateDetailText(FileCount, FolderCount);
            Actions = CreateActions();
        }

        public static CottonFileSelectionSnapshot Empty { get; } =
            new CottonFileSelectionSnapshot(Array.Empty<CottonFileBrowserEntry>());

        public IReadOnlyList<CottonFileBrowserEntry> Entries { get; }

        public int Count { get; }

        public int FileCount { get; }

        public int FolderCount { get; }

        public int LocalFileCount { get; }

        public int OfflineAttentionCount { get; }

        public bool IsActive => Count > 0;

        public bool HasFiles => FileCount > 0;

        public bool HasFolders => FolderCount > 0;

        public bool HasMixedTypes => HasFiles && HasFolders;

        public string TitleText { get; }

        public string DetailText { get; }

        public IReadOnlyList<CottonFileBulkActionSnapshot> Actions { get; }

        public static CottonFileSelectionSnapshot Create(IEnumerable<CottonFileBrowserEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            CottonFileBrowserEntry[] normalizedEntries = entries
                .Where(entry => entry is not null)
                .DistinctBy(entry => entry.Id)
                .ToArray();

            return normalizedEntries.Length == 0
                ? Empty
                : new CottonFileSelectionSnapshot(normalizedEntries);
        }

        public CottonFileBulkActionSnapshot GetAction(CottonFileBulkActionKind kind)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Bulk action kind is unknown.");
            }

            return Actions.First(action => action.Kind == kind);
        }

        private IReadOnlyList<CottonFileBulkActionSnapshot> CreateActions()
        {
            if (!IsActive)
            {
                return Array.Empty<CottonFileBulkActionSnapshot>();
            }

            bool hasFiles = FileCount > 0;
            bool hasOfflineFile = LocalFileCount > 0 || OfflineAttentionCount > 0;

            string copyLinkLabel = Count == 1 ? "Copy link" : "Copy links";
            string shareLinkLabel = Count == 1 ? "Share link" : "Share links";
            string downloadFilesLabel = FileCount == 1 ? "Download file" : "Download files";
            string shareFilesLabel = FileCount == 1 ? "Share file" : "Share files";

            return
            [
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.CopyLinks,
                    copyLinkLabel,
                    isEnabled: true,
                    disabledReason: string.Empty),
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.ShareLinks,
                    shareLinkLabel,
                    isEnabled: true,
                    disabledReason: string.Empty),
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.DownloadFiles,
                    downloadFilesLabel,
                    hasFiles,
                    hasFiles ? string.Empty : SelectFilesOnlyReason),
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.KeepOffline,
                    "Keep offline",
                    isEnabled: true,
                    disabledReason: string.Empty),
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.RemoveOffline,
                    "Remove offline",
                    hasFiles && hasOfflineFile,
                    ResolveRemoveOfflineDisabledReason(hasFiles, hasOfflineFile)),
                new CottonFileBulkActionSnapshot(
                    CottonFileBulkActionKind.ShareLocalFiles,
                    shareFilesLabel,
                    hasFiles && LocalFileCount == FileCount,
                    ResolveShareLocalFilesDisabledReason(hasFiles, LocalFileCount == FileCount)),
            ];
        }

        private static string CreateDetailText(int fileCount, int folderCount)
        {
            if (fileCount == 0 && folderCount == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (fileCount > 0)
            {
                parts.Add(fileCount == 1 ? "1 file" : $"{fileCount} files");
            }

            if (folderCount > 0)
            {
                parts.Add(folderCount == 1 ? "1 folder" : $"{folderCount} folders");
            }

            return string.Join(" · ", parts);
        }

        private static string ResolveRemoveOfflineDisabledReason(bool fileOnlySelection, bool hasOfflineFile)
        {
            if (!fileOnlySelection)
            {
                return SelectFilesOnlyReason;
            }

            return hasOfflineFile ? string.Empty : OfflineFilesOnlyReason;
        }

        private static string ResolveShareLocalFilesDisabledReason(bool fileOnlySelection, bool allFilesLocal)
        {
            if (!fileOnlySelection)
            {
                return SelectFilesOnlyReason;
            }

            return allFilesLocal ? string.Empty : LocalFilesOnlyReason;
        }
    }
}
