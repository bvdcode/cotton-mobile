// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootRunCapability
    {
        public static bool CanRun(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CottonDeviceToCloudSyncRootCapability.CanRun(root);
        }

        public static bool HasUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root);
        }

        public static IReadOnlyList<CottonSyncRootSnapshot> GetRunnableRoots(
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            IReadOnlySet<Guid> pausedRootIds)
        {
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentNullException.ThrowIfNull(pausedRootIds);

            return [.. roots.Where(root => !pausedRootIds.Contains(root.Id) && CanRun(root))];
        }

        public static string CreateUnsupportedLocalRootStatusText(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText;
        }
    }
}
