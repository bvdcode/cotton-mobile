// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupScanner
    {
        Task<CottonCameraBackupScanResult> ScanAsync(
            CottonCameraBackupSettings settings,
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> uploadedMedia,
            CancellationToken cancellationToken = default);
    }
}
