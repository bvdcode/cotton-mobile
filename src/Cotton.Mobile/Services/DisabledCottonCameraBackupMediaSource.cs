// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonCameraBackupMediaSource :
        ICottonCameraBackupMediaSource,
        ICottonCameraBackupMediaContentSource
    {
        public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CottonCameraBackupCandidate>>(
                Array.Empty<CottonCameraBackupCandidate>());
        }

        public Task<Stream?> OpenReadAsync(
            CottonCameraBackupCandidate candidate,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<Stream?>(null);
        }
    }
}
