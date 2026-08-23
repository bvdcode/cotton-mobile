// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAutomaticSyncSessionService
    {
        void Initialize();

        Task SetSessionAsync(
            CottonAuthenticatedSessionScope? sessionScope,
            CancellationToken cancellationToken = default);
    }
}
