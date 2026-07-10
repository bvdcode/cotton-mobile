// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionListDisplayState
    {
        private CottonAccountSessionListDisplayState(
            IReadOnlyList<CottonAccountSessionListItem> items,
            string? currentSessionId,
            string statusText,
            string detailText)
        {
            Items = items;
            CurrentSessionId = currentSessionId;
            StatusText = statusText;
            DetailText = detailText;
        }

        public string Title => "Devices and sessions";

        public IReadOnlyList<CottonAccountSessionListItem> Items { get; }

        public string? CurrentSessionId { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool HasItems => Items.Count > 0;

        public bool CanRevokeCurrentSession => !string.IsNullOrWhiteSpace(CurrentSessionId);

        public string CurrentSessionRevokeActionText => "Revoke current session";

        public static CottonAccountSessionListDisplayState Create(
            IEnumerable<CottonAccountSessionSnapshot> sessions)
        {
            ArgumentNullException.ThrowIfNull(sessions);

            CottonAccountSessionListItem[] items = sessions
                .OrderByDescending(session => session.IsCurrentSession)
                .ThenByDescending(session => session.LastSeenAt)
                .Select(session => new CottonAccountSessionListItem(session))
                .ToArray();
            string? currentSessionId = items
                .FirstOrDefault(item => item.IsCurrentSession)
                ?.Session
                .SessionId;

            return new CottonAccountSessionListDisplayState(
                items,
                currentSessionId,
                items.Length == 0 ? "No active sessions" : CreateStatusText(items.Length),
                string.Empty);
        }

        public static CottonAccountSessionListDisplayState Unavailable(string detailText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            return new CottonAccountSessionListDisplayState(
                Array.Empty<CottonAccountSessionListItem>(),
                currentSessionId: null,
                "Unavailable",
                detailText.Trim());
        }

        private static string CreateStatusText(int count)
        {
            return count == 1 ? "1 active" : count.ToString("N0", CultureInfo.InvariantCulture) + " active";
        }
    }
}
