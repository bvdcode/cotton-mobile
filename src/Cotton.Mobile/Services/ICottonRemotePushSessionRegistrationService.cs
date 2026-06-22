// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushSessionRegistrationService
    {
        Task RegisterCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task RefreshCurrentSessionBestEffortAsync(
            CancellationToken cancellationToken = default);

        Task RevokeCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
