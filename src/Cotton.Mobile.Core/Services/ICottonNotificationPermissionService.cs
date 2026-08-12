// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationPermissionService
    {
        Task<bool> CanPostAsync(CancellationToken cancellationToken = default);

        Task RequestIfNeededAsync(CancellationToken cancellationToken = default);
    }
}
