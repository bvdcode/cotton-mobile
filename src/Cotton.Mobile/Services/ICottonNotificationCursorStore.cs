// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationCursorStore
    {
        Task<CottonNotificationCursor?> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(CottonNotificationCursor cursor, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
