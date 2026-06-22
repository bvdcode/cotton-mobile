// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonCameraBackupMediaAccessPolicy : ICottonCameraBackupMediaAccessPolicy
    {
        public Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonCameraBackupMediaAccessState.NotRequested);
        }

        public Task<CottonCameraBackupMediaAccessState> RequestAccessAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonCameraBackupMediaAccessState.Unavailable);
        }

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }
    }
}
