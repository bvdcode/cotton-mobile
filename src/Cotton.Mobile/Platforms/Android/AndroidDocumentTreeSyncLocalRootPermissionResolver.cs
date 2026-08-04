// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeSyncLocalRootPermissionResolver :
        ICottonSyncLocalRootPermissionResolver
    {
        public CottonSyncRootPermissionStatus Resolve(CottonSyncLocalRootSnapshot localRoot)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!localRoot.RequiresPersistedUserGrant)
            {
                return localRoot.PermissionStatus;
            }

            ContentResolver contentResolver = Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
            bool hasReadWriteGrant = contentResolver.PersistedUriPermissions.Any(permission =>
                permission.IsReadPermission
                && permission.IsWritePermission
                && string.Equals(
                    permission.Uri?.ToString(),
                    localRoot.RootKey,
                    StringComparison.Ordinal));
            if (hasReadWriteGrant)
            {
                return CottonSyncRootPermissionStatus.Available;
            }

            return CottonSyncRootPermissionStatus.Revoked;
        }
    }
}
#endif
