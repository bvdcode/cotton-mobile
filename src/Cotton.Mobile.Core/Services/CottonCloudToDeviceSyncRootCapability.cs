// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncRootCapability
    {
        public static string UnsupportedLocalRootStatusText => CoreResources.LocalSyncTargetUnsupported;

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
            return root.LocalRoot.UsesAppPrivateStorage;
        }
    }
}
