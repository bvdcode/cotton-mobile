// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonLocalFileFreshness
    {
        private static readonly TimeSpan TimestampTolerance = TimeSpan.FromSeconds(2);

        public static bool IsFresh(DateTime localUpdatedAtUtc, DateTime remoteUpdatedAtUtc)
        {
            DateTime localUpdated = NormalizeUtc(localUpdatedAtUtc);
            DateTime remoteUpdated = NormalizeUtc(remoteUpdatedAtUtc);
            return localUpdated.Add(TimestampTolerance) >= remoteUpdated;
        }

        public static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return value.ToUniversalTime();
        }
    }
}
