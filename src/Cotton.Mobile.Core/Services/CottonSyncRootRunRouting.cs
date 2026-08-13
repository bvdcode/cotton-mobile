// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootRunRouting
    {
        public static string CreateStartingStatus(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CottonDeviceToCloudSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName);
        }

        public static string CreateOfflineUnavailableStatus(CottonSyncDirection direction)
        {
            EnsureSupported(direction);
            return CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus;
        }

        public static string CreateFailedStatus(CottonSyncDirection direction)
        {
            EnsureSupported(direction);
            return CottonDeviceToCloudSyncStatusText.FailedStatus;
        }

        private static void EnsureSupported(CottonSyncDirection direction)
        {
            if (direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }
        }
    }
}
