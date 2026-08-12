// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationCursor
    {
        public CottonNotificationCursor(Guid? lastNotificationId, int totalCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

            LastNotificationId = lastNotificationId;
            TotalCount = totalCount;
        }

        public Guid? LastNotificationId { get; }

        public int TotalCount { get; }
    }
}
