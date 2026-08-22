// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public partial class AndroidDocumentTreeDeviceToCloudLocalTreeReader
    {
        private static AndroidUri ParseTreeUri(CottonSyncRootSnapshot root)
        {
            AndroidUri? uri = AndroidUri.Parse(root.LocalRoot.RootKey);
            return uri ?? throw new InvalidOperationException("Document-tree sync root URI is invalid.");
        }

        private static AndroidUri GetRootDocumentUri(AndroidUri treeUri)
        {
            string rootDocumentId = DocumentsContract.GetTreeDocumentId(treeUri)
                ?? throw new InvalidOperationException("Document-tree root id is unavailable.");
            return DocumentsContract.BuildDocumentUriUsingTree(treeUri, rootDocumentId)
                ?? throw new InvalidOperationException("Could not build document-tree root URI.");
        }

        private static ContentResolver GetContentResolver()
        {
            return global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
        }

        private static void EnsureSupportedRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);

            if (!string.Equals(
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri),
                CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Device-to-cloud sync root is not ready.");
            }

            if (!root.LocalRoot.RequiresPersistedUserGrant)
            {
                throw new InvalidOperationException(
                    "Device-to-cloud local tree reading only supports user-selected folders.");
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException(
                    "Device-to-cloud local tree reading requires device-to-cloud sync direction.");
            }
        }
    }
}
#endif
