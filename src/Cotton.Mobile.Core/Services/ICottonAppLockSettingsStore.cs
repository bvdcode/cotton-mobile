// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAppLockSettingsStore
    {
        Task<CottonAppLockSettings> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(CottonAppLockSettings settings, CancellationToken cancellationToken = default);
    }
}
