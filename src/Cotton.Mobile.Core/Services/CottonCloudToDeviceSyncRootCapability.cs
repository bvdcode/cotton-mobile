namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncRootCapability
    {
        public const string UnsupportedLocalRootStatusText = "Local sync target unsupported";

        public static bool CanRun(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction == CottonSyncDirection.CloudToDevice
                && root.CanRunSync
                && HasSupportedLocalRoot(root);
        }

        public static bool HasUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return root.Direction == CottonSyncDirection.CloudToDevice
                && root.CanRunSync
                && !HasSupportedLocalRoot(root);
        }

        private static bool HasSupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            return root.LocalRoot.UsesAppPrivateStorage || root.LocalRoot.RequiresPersistedUserGrant;
        }
    }
}
