namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootRunCapability
    {
        public static bool CanRun(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction switch
            {
                CottonSyncDirection.CloudToDevice => CottonCloudToDeviceSyncRootCapability.CanRun(root),
                CottonSyncDirection.DeviceToCloud => CottonDeviceToCloudSyncRootCapability.CanRun(root),
                CottonSyncDirection.Bidirectional => false,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    "Sync direction is not supported."),
            };
        }

        public static bool HasUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction switch
            {
                CottonSyncDirection.CloudToDevice =>
                    CottonCloudToDeviceSyncRootCapability.HasUnsupportedLocalRoot(root),
                CottonSyncDirection.DeviceToCloud =>
                    CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root),
                CottonSyncDirection.Bidirectional => false,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    "Sync direction is not supported."),
            };
        }

        public static string CreateUnsupportedLocalRootStatusText(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction switch
            {
                CottonSyncDirection.CloudToDevice =>
                    CottonCloudToDeviceSyncRootCapability.UnsupportedLocalRootStatusText,
                CottonSyncDirection.DeviceToCloud =>
                    CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText,
                CottonSyncDirection.Bidirectional => CottonBidirectionalSyncStatusText.ExecutionUnavailableStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    "Sync direction is not supported."),
            };
        }
    }
}
