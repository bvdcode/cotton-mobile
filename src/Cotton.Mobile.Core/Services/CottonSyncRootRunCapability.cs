namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootRunCapability
    {
        public static bool CanRun(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CottonCloudToDeviceSyncRootCapability.CanRun(root)
                || CottonDeviceToCloudSyncRootCapability.CanRun(root);
        }

        public static bool HasUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction switch
            {
                CottonSyncDirection.CloudToDevice =>
                    CottonCloudToDeviceSyncRootCapability.HasUnsupportedLocalRoot(root),
                CottonSyncDirection.DeviceToCloud or CottonSyncDirection.Bidirectional =>
                    CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root),
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
                CottonSyncDirection.DeviceToCloud or CottonSyncDirection.Bidirectional =>
                    CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    "Sync direction is not supported."),
            };
        }
    }
}
