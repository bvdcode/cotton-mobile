// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonMediaLocationPermissionService
    {
        bool IsSupported { get; }

        bool IsGranted { get; }

        bool CanRequest { get; }

        Task RequestAsync(CancellationToken cancellationToken = default);
    }
}
