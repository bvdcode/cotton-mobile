using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionListDisplayState
    {
        private CottonAccountSessionListDisplayState(
            IReadOnlyList<CottonAccountSessionListItem> items,
            string statusText,
            string detailText,
            string emptyTitle,
            string emptyDetails)
        {
            Items = items;
            StatusText = statusText;
            DetailText = detailText;
            EmptyTitle = emptyTitle;
            EmptyDetails = emptyDetails;
        }

        public string Title => "Devices and sessions";

        public IReadOnlyList<CottonAccountSessionListItem> Items { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public string EmptyTitle { get; }

        public string EmptyDetails { get; }

        public bool HasItems => Items.Count > 0;

        public bool IsEmptyVisible => !HasItems;

        public static CottonAccountSessionListDisplayState Create(
            IEnumerable<CottonAccountSessionSnapshot> sessions)
        {
            ArgumentNullException.ThrowIfNull(sessions);

            CottonAccountSessionListItem[] items = sessions
                .OrderByDescending(session => session.IsCurrentSession)
                .ThenByDescending(session => session.LastSeenAt)
                .Select(session => new CottonAccountSessionListItem(session))
                .ToArray();

            return new CottonAccountSessionListDisplayState(
                items,
                CreateStatusText(items.Length),
                items.Length == 0
                    ? "No active account sessions were returned by the server."
                    : "Signed-in account sessions reported by the server.",
                "No active sessions",
                "The server did not return any active account sessions.");
        }

        public static CottonAccountSessionListDisplayState Unavailable(string detailText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            return new CottonAccountSessionListDisplayState(
                Array.Empty<CottonAccountSessionListItem>(),
                "Unavailable",
                detailText.Trim(),
                "Sessions unavailable",
                detailText.Trim());
        }

        private static string CreateStatusText(int count)
        {
            return count == 1 ? "1 active" : count.ToString("N0", CultureInfo.InvariantCulture) + " active";
        }
    }
}
