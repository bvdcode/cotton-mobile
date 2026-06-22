// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootRunRouting
    {
        public static CottonSyncRootRunRoute CreateRoute(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CreateRoute(root.Direction);
        }

        public static CottonSyncRootRunRoute CreateRoute(CottonSyncDirection direction)
        {
            return direction switch
            {
                CottonSyncDirection.CloudToDevice => CottonSyncRootRunRoute.CloudToDevice,
                CottonSyncDirection.DeviceToCloud => CottonSyncRootRunRoute.DeviceToCloud,
                CottonSyncDirection.Bidirectional => CottonSyncRootRunRoute.Bidirectional,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "Sync direction is not supported."),
            };
        }

        public static string CreateStartingStatus(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CreateRoute(root) switch
            {
                CottonSyncRootRunRoute.CloudToDevice =>
                    CottonCloudToDeviceSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName),
                CottonSyncRootRunRoute.DeviceToCloud =>
                    CottonDeviceToCloudSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName),
                CottonSyncRootRunRoute.Bidirectional =>
                    CottonBidirectionalSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    "Sync run route is not supported."),
            };
        }

        public static string CreateOfflineUnavailableStatus(CottonSyncDirection direction)
        {
            return CreateRoute(direction) switch
            {
                CottonSyncRootRunRoute.CloudToDevice => CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus,
                CottonSyncRootRunRoute.DeviceToCloud => CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus,
                CottonSyncRootRunRoute.Bidirectional => CottonBidirectionalSyncStatusText.OfflineUnavailableStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "Sync run route is not supported."),
            };
        }

        public static string CreateFailedStatus(CottonSyncDirection direction)
        {
            return CreateRoute(direction) switch
            {
                CottonSyncRootRunRoute.CloudToDevice => CottonCloudToDeviceSyncStatusText.FailedStatus,
                CottonSyncRootRunRoute.DeviceToCloud => CottonDeviceToCloudSyncStatusText.FailedStatus,
                CottonSyncRootRunRoute.Bidirectional => CottonBidirectionalSyncStatusText.FailedStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "Sync run route is not supported."),
            };
        }
    }
}
