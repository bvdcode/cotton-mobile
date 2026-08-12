// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncRootCapability
    {
        public static string UnsupportedLocalRootStatusText => CoreResources.LocalSyncSourceUnsupported;

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
