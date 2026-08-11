// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonCloudToDeviceSyncRootValidator
    {
        public static void EnsureSupported(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonSyncRootStorageKind expectedStorageKind)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);
            if (!Enum.IsDefined(expectedStorageKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedStorageKind),
                    "Sync root storage kind is not supported.");
            }

            string instanceKey = CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri);
            string rootInstanceKey = CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri);
            if (!string.Equals(instanceKey, rootInstanceKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cloud-to-device sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Cloud-to-device sync root is not ready.");
            }

            if (root.LocalRoot.StorageKind != expectedStorageKind)
            {
                throw new InvalidOperationException(CreateStorageError(expectedStorageKind));
            }

            if (root.Direction == CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("This sync file operator requires cloud-to-device sync direction.");
            }
        }

        private static string CreateStorageError(CottonSyncRootStorageKind storageKind)
        {
            return storageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory =>
                    "This sync file operator only supports app-private local roots.",
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    "This sync file operator only supports user-selected folders.",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(storageKind),
                    storageKind,
                    "Sync root storage kind is not supported."),
            };
        }
    }
}
