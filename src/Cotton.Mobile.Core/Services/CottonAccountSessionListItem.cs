// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionListItem
    {
        public CottonAccountSessionListItem(CottonAccountSessionSnapshot session)
        {
            ArgumentNullException.ThrowIfNull(session);

            Session = session;
            Title = session.Device;
            BadgeText = session.IsCurrentSession ? "Current" : "Active";
            DetailText = CreateDetailText(session);
            AccessText = CottonAccountSessionAuthTypeText.Format(session.AuthType)
                + " - "
                + CreateLastSeenText(session.LastSeenAt);
            DurationText = FormatDuration(session.TotalSessionDuration)
                + " active - "
                + FormatRefreshTokenCount(session.RefreshTokenCount);
        }

        public CottonAccountSessionSnapshot Session { get; }

        public string Title { get; }

        public string BadgeText { get; }

        public string DetailText { get; }

        public string AccessText { get; }

        public string DurationText { get; }

        public bool IsCurrentSession => Session.IsCurrentSession;

        private static string CreateDetailText(CottonAccountSessionSnapshot session)
        {
            string location = CreateLocationText(session);
            return session.IpAddress == "Unknown IP"
                ? location
                : location + " - " + session.IpAddress;
        }

        private static string CreateLocationText(CottonAccountSessionSnapshot session)
        {
            string[] parts =
            [
                session.City,
                session.Region,
                session.Country,
            ];
            string[] knownParts = parts
                .Where(part => !string.Equals(part, "Unknown", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return knownParts.Length == 0 ? "Unknown location" : string.Join(", ", knownParts);
        }

        private static string CreateLastSeenText(DateTime lastSeenAt)
        {
            DateTime utc = lastSeenAt.Kind == DateTimeKind.Utc
                ? lastSeenAt
                : lastSeenAt.ToUniversalTime();
            return "Last seen " + utc.ToString("MMM d, yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.FromMinutes(1))
            {
                return "Less than 1 min";
            }

            if (duration < TimeSpan.FromHours(1))
            {
                int minutes = Math.Max(1, (int)Math.Round(duration.TotalMinutes));
                return FormatUnit(minutes, "min");
            }

            if (duration < TimeSpan.FromDays(1))
            {
                int hours = Math.Max(1, (int)Math.Round(duration.TotalHours));
                return FormatUnit(hours, "hour");
            }

            int days = Math.Max(1, (int)Math.Round(duration.TotalDays));
            return FormatUnit(days, "day");
        }

        private static string FormatRefreshTokenCount(int refreshTokenCount)
        {
            return FormatUnit(refreshTokenCount, "token");
        }

        private static string FormatUnit(int value, string unit)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture)
                + " "
                + unit
                + (value == 1 ? string.Empty : "s");
        }
    }
}
