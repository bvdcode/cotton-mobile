// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAppLockRuntimeStateStore
    {
        Task<CottonAppLockRuntimeState> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(
            CottonAppLockRuntimeState runtimeState,
            CancellationToken cancellationToken = default);
    }
}
