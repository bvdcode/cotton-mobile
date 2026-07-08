// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupSettings
    {
        public static CottonCameraBackupSettings Default { get; } =
            new(false, null, photosOnly: true, wifiOnly: true, allowCellular: false, chargingOnly: false);

        public CottonCameraBackupSettings(
            bool isEnabled,
            CottonUploadDestinationSnapshot? destination,
            bool photosOnly,
            bool wifiOnly,
            bool allowCellular,
            bool chargingOnly)
        {
            if (isEnabled
                && (destination is null
                    || CottonSelectedMediaTransferPolicy.RequiresDurableQueueBeforeCameraBackup))
            {
                throw new InvalidOperationException(
                    "Camera backup cannot be enabled until a destination and durable queued uploads are available.");
            }

            IsEnabled = isEnabled;
            Destination = destination;
            PhotosOnly = photosOnly;
            WifiOnly = allowCellular ? false : wifiOnly;
            AllowCellular = allowCellular && !WifiOnly;
            ChargingOnly = chargingOnly;
        }

        public bool IsEnabled { get; }

        public CottonUploadDestinationSnapshot? Destination { get; }

        public bool PhotosOnly { get; }

        public bool WifiOnly { get; }

        public bool AllowCellular { get; }

        public bool ChargingOnly { get; }

        public bool HasDestination => Destination is not null;

        public bool CanRunBackup =>
            HasDestination && !CottonSelectedMediaTransferPolicy.RequiresDurableQueueBeforeCameraBackup;

        public CottonCameraBackupSettings WithDestination(CottonUploadDestinationSnapshot? destination)
        {
            return new CottonCameraBackupSettings(
                isEnabled: IsEnabled && destination is not null,
                destination,
                PhotosOnly,
                WifiOnly,
                AllowCellular,
                ChargingOnly);
        }

        public CottonCameraBackupSettings WithEnabled(bool isEnabled)
        {
            return new CottonCameraBackupSettings(
                isEnabled,
                Destination,
                PhotosOnly,
                WifiOnly,
                AllowCellular,
                ChargingOnly);
        }

        public CottonCameraBackupSettings WithPhotosOnly(bool photosOnly)
        {
            return new CottonCameraBackupSettings(
                IsEnabled,
                Destination,
                photosOnly,
                WifiOnly,
                AllowCellular,
                ChargingOnly);
        }

        public CottonCameraBackupSettings WithWifiOnly(bool wifiOnly)
        {
            return new CottonCameraBackupSettings(
                IsEnabled,
                Destination,
                PhotosOnly,
                wifiOnly,
                allowCellular: wifiOnly ? false : AllowCellular,
                ChargingOnly);
        }

        public CottonCameraBackupSettings WithAllowCellular(bool allowCellular)
        {
            return new CottonCameraBackupSettings(
                IsEnabled,
                Destination,
                PhotosOnly,
                wifiOnly: allowCellular ? false : WifiOnly,
                allowCellular,
                ChargingOnly);
        }

        public CottonCameraBackupSettings WithChargingOnly(bool chargingOnly)
        {
            return new CottonCameraBackupSettings(
                IsEnabled,
                Destination,
                PhotosOnly,
                WifiOnly,
                AllowCellular,
                chargingOnly);
        }
    }
}
