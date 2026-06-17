namespace Cotton.Mobile.Services
{
    public interface IFileBrowserPreferenceStore
    {
        CottonFileBrowserPreferences Get();

        void SaveViewMode(CottonFileBrowserViewMode viewMode);

        void SaveSortMode(CottonFileBrowserSortMode sortMode);
    }
}
