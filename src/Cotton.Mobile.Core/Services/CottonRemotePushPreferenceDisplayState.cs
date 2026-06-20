using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPreferenceDisplayState
    {
        private CottonRemotePushPreferenceDisplayState(
            string summaryText,
            IReadOnlyList<CottonRemotePushPreferenceDisplayItem> items)
        {
            SummaryText = summaryText;
            Items = items;
        }

        public string SummaryText { get; }

        public IReadOnlyList<CottonRemotePushPreferenceDisplayItem> Items { get; }

        public int EnabledCategoryCount => Items.Count(item => item.IsEnabled);

        public static CottonRemotePushPreferenceDisplayState Create(CottonRemotePushPreferences preferences)
        {
            return Create(
                preferences,
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend);
        }

        public static CottonRemotePushPreferenceDisplayState Create(
            CottonRemotePushPreferences preferences,
            CottonRemotePushCapabilitySnapshot capability)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(capability);

            CottonRemotePushPreferenceDisplayItem[] items = capability.EventCategories
                .Select(category => CreateItem(preferences, category.Category))
                .ToArray();

            return new CottonRemotePushPreferenceDisplayState(
                FormatSummary(items.Count(item => item.IsEnabled)),
                items);
        }

        private static CottonRemotePushPreferenceDisplayItem CreateItem(
            CottonRemotePushPreferences preferences,
            CottonRemotePushEventCategory category)
        {
            return category switch
            {
                CottonRemotePushEventCategory.SharedFile => new CottonRemotePushPreferenceDisplayItem(
                    CottonRemotePushEventCategory.SharedFile,
                    "Shared-file activity",
                    "Shared links and file access activity.",
                    preferences.SharedFile),
                CottonRemotePushEventCategory.SecuritySession => new CottonRemotePushPreferenceDisplayItem(
                    CottonRemotePushEventCategory.SecuritySession,
                    "Security and sessions",
                    "Sign-in and account security alerts.",
                    preferences.SecuritySession),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(category),
                    "Remote push category cannot be displayed."),
            };
        }

        private static string FormatSummary(int enabledCategoryCount)
        {
            return enabledCategoryCount == 1
                ? "1 server alert on"
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:N0} server alerts on",
                    enabledCategoryCount);
        }
    }
}
