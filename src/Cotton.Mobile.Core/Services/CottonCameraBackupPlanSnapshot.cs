// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupPlanSnapshot
    {
        public CottonCameraBackupPlanSnapshot(
            CottonCameraBackupScanResult scanResult,
            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> uploadedMedia,
            CottonCameraBackupHealthSnapshot health)
        {
            ArgumentNullException.ThrowIfNull(scanResult);
            ArgumentNullException.ThrowIfNull(uploadedMedia);
            ArgumentNullException.ThrowIfNull(health);

            ScanResult = scanResult;
            UploadedMedia = uploadedMedia.ToArray();
            Health = health;
            DestinationStorageEstimate = CottonCameraBackupDestinationStorageEstimate.Create(scanResult);
        }

        public CottonCameraBackupScanResult ScanResult { get; }

        public IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> UploadedMedia { get; }

        public CottonCameraBackupHealthSnapshot Health { get; }

        public CottonCameraBackupDestinationStorageEstimate DestinationStorageEstimate { get; }
    }
}
