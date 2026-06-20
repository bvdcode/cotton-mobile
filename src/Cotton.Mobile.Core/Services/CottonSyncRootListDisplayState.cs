namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListDisplayState
    {
        private CottonSyncRootListDisplayState(IReadOnlyList<CottonSyncRootListItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            Items = items;
            SummaryText = CreateSummaryText(items.Count);
        }

        public IReadOnlyList<CottonSyncRootListItem> Items { get; }

        public string SummaryText { get; }

        public bool HasItems => Items.Count > 0;

        public bool IsEmptyVisible => !HasItems;

        public static CottonSyncRootListDisplayState Create(IReadOnlyList<CottonSyncRootSnapshot> roots)
        {
            ArgumentNullException.ThrowIfNull(roots);

            return new CottonSyncRootListDisplayState(
                roots
                    .OrderBy(root => root.CloudFolder.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(root => root.CloudFolder.FolderName, StringComparer.OrdinalIgnoreCase)
                    .Select(root => new CottonSyncRootListItem(root))
                    .ToArray());
        }

        private static string CreateSummaryText(int count)
        {
            return count switch
            {
                0 => "No folders syncing",
                1 => "1 folder set to sync",
                _ => $"{count} folders set to sync",
            };
        }
    }
}
