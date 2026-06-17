namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserPreferences
    {
        public CottonFileBrowserPreferences(
            CottonFileBrowserViewMode viewMode,
            CottonFileBrowserSortMode sortMode)
        {
            ViewMode = viewMode;
            SortMode = sortMode;
        }

        public CottonFileBrowserViewMode ViewMode { get; }

        public CottonFileBrowserSortMode SortMode { get; }
    }
}
