namespace Cotton.Mobile.Services
{
    public class CottonTrashSelectionSnapshot
    {
        private CottonTrashSelectionSnapshot(IReadOnlyList<CottonFileBrowserEntry> entries)
        {
            Entries = entries;
            Count = entries.Count;
            FileCount = entries.Count(entry => entry.Type == CottonFileBrowserEntryType.File);
            FolderCount = entries.Count(entry => entry.Type == CottonFileBrowserEntryType.Folder);
            TitleText = Count == 1 ? "1 selected" : $"{Count:N0} selected";
            DetailText = CreateDetailText(FileCount, FolderCount);
            Actions = CreateActions();
        }

        public static CottonTrashSelectionSnapshot Empty { get; } =
            new CottonTrashSelectionSnapshot(Array.Empty<CottonFileBrowserEntry>());

        public IReadOnlyList<CottonFileBrowserEntry> Entries { get; }

        public int Count { get; }

        public int FileCount { get; }

        public int FolderCount { get; }

        public bool IsActive => Count > 0;

        public bool HasFiles => FileCount > 0;

        public bool HasFolders => FolderCount > 0;

        public bool HasMixedTypes => HasFiles && HasFolders;

        public string TitleText { get; }

        public string DetailText { get; }

        public IReadOnlyList<CottonTrashBulkActionSnapshot> Actions { get; }

        public static CottonTrashSelectionSnapshot Create(IEnumerable<CottonFileBrowserEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            CottonFileBrowserEntry[] normalizedEntries = entries
                .Where(entry => entry is not null)
                .DistinctBy(entry => entry.Id)
                .ToArray();

            return normalizedEntries.Length == 0
                ? Empty
                : new CottonTrashSelectionSnapshot(normalizedEntries);
        }

        public CottonTrashBulkActionSnapshot GetAction(CottonTrashBulkActionKind kind)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Trash bulk action kind is unknown.");
            }

            return Actions.First(action => action.Kind == kind);
        }

        private IReadOnlyList<CottonTrashBulkActionSnapshot> CreateActions()
        {
            if (!IsActive)
            {
                return Array.Empty<CottonTrashBulkActionSnapshot>();
            }

            return
            [
                new CottonTrashBulkActionSnapshot(
                    CottonTrashBulkActionKind.Restore,
                    CottonTrashBulkStatusText.RestoreAction,
                    isEnabled: true,
                    disabledReason: string.Empty),
                new CottonTrashBulkActionSnapshot(
                    CottonTrashBulkActionKind.DeleteForever,
                    CottonTrashBulkStatusText.DeleteForeverAction,
                    isEnabled: true,
                    disabledReason: string.Empty),
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
                parts.Add(fileCount == 1 ? "1 file" : $"{fileCount:N0} files");
            }

            if (folderCount > 0)
            {
                parts.Add(folderCount == 1 ? "1 folder" : $"{folderCount:N0} folders");
            }

            return string.Join(" · ", parts);
        }
    }
}
