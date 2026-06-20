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

        public static CottonRemotePushPreferenceDisplayState Create(CottonRemotePushPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            return new CottonRemotePushPreferenceDisplayState(
                FormatSummary(preferences.EnabledCategoryCount),
                [
                    new CottonRemotePushPreferenceDisplayItem(
                        CottonRemotePushEventCategory.SharedFile,
                        "Shared-file activity",
                        "Shared links and file access activity.",
                        preferences.SharedFile),
                    new CottonRemotePushPreferenceDisplayItem(
                        CottonRemotePushEventCategory.AccessRequest,
                        "Access requests",
                        "Requests for access to shared content.",
                        preferences.AccessRequest),
                    new CottonRemotePushPreferenceDisplayItem(
                        CottonRemotePushEventCategory.CommentMention,
                        "Comments and mentions",
                        "Collaboration alerts when they are available.",
                        preferences.CommentMention),
                    new CottonRemotePushPreferenceDisplayItem(
                        CottonRemotePushEventCategory.SecuritySession,
                        "Security and sessions",
                        "Sign-in and account security alerts.",
                        preferences.SecuritySession),
                ]);
        }

        private static string FormatSummary(int enabledCategoryCount)
        {
            return enabledCategoryCount == 1
                ? "1 server push category on"
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:N0} server push categories on",
                    enabledCategoryCount);
        }
    }
}
