// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupSettingsStore
    {
        Task<CottonCameraBackupSettings> GetAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
