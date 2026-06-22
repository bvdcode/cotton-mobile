// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonShareLaunchState
    {
        event EventHandler? ShareStaged;

        int PendingShareLaunchCount { get; }

        void NotifyShareStaged();

        bool TryConsumePendingShareLaunch();
    }
}
