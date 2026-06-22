// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaSource
    {
        Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default);
    }
}
