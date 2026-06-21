namespace Cotton.Mobile.Services
{
    public class CottonTrashListSnapshot
    {
        private CottonTrashListSnapshot(IReadOnlyList<CottonFileBrowserEntry> items)
        {
            Items = items;
            SummaryText = CreateSummaryText(items.Count);
        }

        public IReadOnlyList<CottonFileBrowserEntry> Items { get; }

        public string SummaryText { get; }

        public string EmptyMessage => "Trash is empty";

        public string EmptyDetails => "Deleted files and folders will appear here.";

        public bool IsEmpty => Items.Count == 0;

        public bool IsListVisible => !IsEmpty;

        public static CottonTrashListSnapshot Create(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            return new CottonTrashListSnapshot(content.Entries.ToArray());
        }

        public static string CreateSummaryText(int itemCount)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Trash item count cannot be negative.");
            }

            return itemCount switch
            {
                0 => "Trash is empty",
                1 => "1 item in trash",
                _ => $"{itemCount:N0} items in trash",
            };
        }
    }
}
