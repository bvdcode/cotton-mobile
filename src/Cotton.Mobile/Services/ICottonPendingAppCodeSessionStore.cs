// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonPendingAppCodeSessionStore
    {
        Task<CottonPendingAppCodeSession?> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(CottonPendingAppCodeSession session, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
