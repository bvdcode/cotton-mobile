// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaAccessPolicy
    {
        Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default);

        Task<CottonCameraBackupMediaAccessState> RequestAccessAsync(
            CancellationToken cancellationToken = default);

        Task OpenSettingsAsync(CancellationToken cancellationToken = default);

        Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default);
    }
}
