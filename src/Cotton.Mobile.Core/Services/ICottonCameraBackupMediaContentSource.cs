// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaContentSource
    {
        Task<Stream?> OpenReadAsync(
            CottonCameraBackupCandidate candidate,
            CancellationToken cancellationToken = default);
    }
}
