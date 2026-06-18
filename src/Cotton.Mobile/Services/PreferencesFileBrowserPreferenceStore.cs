using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesFileBrowserPreferenceStore : IFileBrowserPreferenceStore
    {
        private const string ViewModeKey = "FileBrowser.ViewMode";
        private const string SortModeKey = "FileBrowser.SortMode";

        private readonly IPreferences _preferences;

        public PreferencesFileBrowserPreferenceStore(IPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            _preferences = preferences;
        }

        public CottonFileBrowserPreferences Get()
        {
            return new CottonFileBrowserPreferences(
                GetEnum(ViewModeKey, CottonFileBrowserViewMode.List),
                GetEnum(SortModeKey, CottonFileBrowserSortMode.Name));
        }

        public void SaveViewMode(CottonFileBrowserViewMode viewMode)
        {
            _preferences.Set(ViewModeKey, viewMode.ToString());
        }

        public void SaveSortMode(CottonFileBrowserSortMode sortMode)
        {
            _preferences.Set(SortModeKey, sortMode.ToString());
        }

        private TEnum GetEnum<TEnum>(string key, TEnum defaultValue)
            where TEnum : struct, Enum
        {
            string defaultText = defaultValue.ToString() ?? string.Empty;
            string value = _preferences.Get(key, defaultText) ?? defaultText;
            if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
                && Enum.IsDefined(typeof(TEnum), parsed))
            {
                return parsed;
            }

            _preferences.Remove(key);
            return defaultValue;
        }
    }
}
