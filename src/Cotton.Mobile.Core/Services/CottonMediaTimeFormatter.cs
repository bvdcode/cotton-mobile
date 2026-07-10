// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public static class CottonMediaTimeFormatter
    {
        public static string Format(TimeSpan value)
        {
            TimeSpan normalized = value < TimeSpan.Zero ? TimeSpan.Zero : value;
            long totalHours = (long)Math.Floor(normalized.TotalHours);

            return totalHours > 0
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1:00}:{2:00}",
                    totalHours,
                    normalized.Minutes,
                    normalized.Seconds)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1:00}",
                    (long)Math.Floor(normalized.TotalMinutes),
                    normalized.Seconds);
        }
    }
}
