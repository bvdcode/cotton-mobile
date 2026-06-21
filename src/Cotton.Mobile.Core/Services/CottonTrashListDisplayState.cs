namespace Cotton.Mobile.Services
{
    public class CottonTrashListDisplayState
    {
        private CottonTrashListDisplayState(
            IReadOnlyList<CottonFileBrowserEntry> items,
            int totalItemCount,
            string searchText,
            bool isSearchOpen,
            CottonFileBrowserSortMode sortMode,
            CottonFileBrowserViewMode viewMode)
        {
            Items = items;
            TotalItemCount = totalItemCount;
            SearchText = searchText;
            IsSearchOpen = isSearchOpen;
            SortMode = sortMode;
            ViewMode = viewMode;
        }

        public IReadOnlyList<CottonFileBrowserEntry> Items { get; }

        public int TotalItemCount { get; }

        public int VisibleItemCount => Items.Count;

        public string SearchText { get; }

        public bool IsSearchOpen { get; }

        public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);

        public CottonFileBrowserSortMode SortMode { get; }

        public CottonFileBrowserViewMode ViewMode { get; }

        public bool IsEmpty => VisibleItemCount == 0;

        public bool IsListVisible => VisibleItemCount > 0 && ViewMode == CottonFileBrowserViewMode.List;

        public bool IsTileVisible => VisibleItemCount > 0 && ViewMode == CottonFileBrowserViewMode.Tiles;

        public bool IsSearchVisible => IsSearchOpen || IsSearchActive;

        public bool IsSortButtonVisible => !IsSearchVisible;

        public bool IsViewButtonVisible => !IsSearchVisible;

        public string SearchButtonText => IsSearchVisible ? "×" : "⌕";

        public string SearchButtonDescription
        {
            get
            {
                if (IsSearchActive)
                {
                    return "Clear trash search";
                }

                return IsSearchOpen ? "Close trash search" : "Search trash";
            }
        }

        public string ViewButtonText => ViewMode == CottonFileBrowserViewMode.List ? "☰" : "▦";

        public string SortButtonText => FormatSortButtonText(SortMode);

        public string SummaryText
        {
            get
            {
                if (TotalItemCount == 0)
                {
                    return CottonTrashListSnapshot.CreateSummaryText(0);
                }

                string count = VisibleItemCount == TotalItemCount
                    ? CottonTrashListSnapshot.CreateSummaryText(TotalItemCount)
                    : CreateFilteredCount(VisibleItemCount);
                return $"{count} · {FormatSortStatus(SortMode)}";
            }
        }

        public string EmptyMessage => TotalItemCount == 0
            ? "Trash is empty"
            : "No trash matches";

        public string EmptyDetails => TotalItemCount == 0
            ? "Deleted files and folders will appear here."
            : "Try another search.";

        public static CottonTrashListDisplayState Create(
            IReadOnlyList<CottonFileBrowserEntry> items,
            string? searchText,
            bool isSearchOpen,
            CottonFileBrowserSortMode sortMode,
            CottonFileBrowserViewMode viewMode)
        {
            ArgumentNullException.ThrowIfNull(items);

            string normalizedSearchText = string.IsNullOrWhiteSpace(searchText)
                ? string.Empty
                : searchText.Trim();
            IReadOnlyList<CottonFileBrowserEntry> visibleItems = SortEntries(
                    items.Where(item => item.Matches(normalizedSearchText)),
                    sortMode)
                .ToArray();
            return new CottonTrashListDisplayState(
                visibleItems,
                items.Count,
                normalizedSearchText,
                isSearchOpen,
                sortMode,
                viewMode);
        }

        public static string FormatSortStatus(CottonFileBrowserSortMode sortMode)
        {
            return sortMode switch
            {
                CottonFileBrowserSortMode.Name => "A-Z",
                CottonFileBrowserSortMode.Updated => "Newest",
                CottonFileBrowserSortMode.Type => "Type",
                CottonFileBrowserSortMode.Size => "Size",
                _ => sortMode.ToString(),
            };
        }

        public static string FormatSortButtonText(CottonFileBrowserSortMode sortMode)
        {
            return sortMode switch
            {
                CottonFileBrowserSortMode.Name => "A-Z",
                CottonFileBrowserSortMode.Updated => "New",
                CottonFileBrowserSortMode.Type => "Type",
                CottonFileBrowserSortMode.Size => "Size",
                _ => sortMode.ToString(),
            };
        }

        private static IEnumerable<CottonFileBrowserEntry> SortEntries(
            IEnumerable<CottonFileBrowserEntry> items,
            CottonFileBrowserSortMode sortMode)
        {
            return sortMode switch
            {
                CottonFileBrowserSortMode.Type => items
                    .OrderBy(item => item.IsFolder ? 0 : 1)
                    .ThenBy(item => item.Kind, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
                CottonFileBrowserSortMode.Updated => items
                    .OrderBy(item => item.IsFolder ? 0 : 1)
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
                CottonFileBrowserSortMode.Size => items
                    .OrderBy(item => item.IsFolder ? 0 : 1)
                    .ThenBy(item => item.IsFolder ? item.Name : string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.SizeBytes.HasValue ? 0 : 1)
                    .ThenByDescending(item => item.SizeBytes ?? 0)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
                _ => items
                    .OrderBy(item => item.IsFolder ? 0 : 1)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            };
        }

        private static string CreateFilteredCount(int visibleCount)
        {
            return visibleCount == 1 ? "1 match" : $"{visibleCount:N0} matches";
        }
    }
}
