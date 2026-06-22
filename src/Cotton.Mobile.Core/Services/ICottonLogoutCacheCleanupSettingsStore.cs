// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonLogoutCacheCleanupSettingsStore
    {
        Task<CottonLogoutCacheCleanupSettings> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(
            CottonLogoutCacheCleanupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
