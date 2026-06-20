namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncRootCapability
    {
        public const string UnsupportedLocalRootStatusText = "Local sync source unsupported";

        public static bool CanRun(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return IsDeviceToCloudDirection(root)
                && root.CanRunSync
                && HasSupportedLocalRoot(root);
        }

        public static bool HasUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return IsDeviceToCloudDirection(root)
                && root.CanRunSync
                && !HasSupportedLocalRoot(root);
        }

        private static bool IsDeviceToCloudDirection(CottonSyncRootSnapshot root)
        {
            return root.Direction is CottonSyncDirection.DeviceToCloud or CottonSyncDirection.Bidirectional;
        }

        private static bool HasSupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            return root.LocalRoot.RequiresPersistedUserGrant;
        }
    }
}
