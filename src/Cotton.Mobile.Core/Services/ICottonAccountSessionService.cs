// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAccountSessionService
    {
        Task<IReadOnlyList<CottonAccountSessionSnapshot>> GetActiveSessionsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(
            Uri instanceUri,
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
