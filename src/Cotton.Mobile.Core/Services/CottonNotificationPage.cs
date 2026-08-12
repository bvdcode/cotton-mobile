// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationPage(
        IReadOnlyList<CottonNotificationDto> notifications,
        int totalCount)
    {
        public IReadOnlyList<CottonNotificationDto> Notifications { get; } =
            notifications ?? throw new ArgumentNullException(nameof(notifications));

        public int TotalCount { get; } = totalCount >= 0
            ? totalCount
            : throw new ArgumentOutOfRangeException(nameof(totalCount));
    }
}
