// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeSyncLocalRootPermissionResolver :
        ICottonSyncLocalRootPermissionResolver
    {
        private static readonly string[] RootProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
        ];

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
            if (!hasReadWriteGrant)
            {
                return CottonSyncRootPermissionStatus.Revoked;
            }

            return IsRootAvailable(contentResolver, localRoot.RootKey)
                ? CottonSyncRootPermissionStatus.Available
                : CottonSyncRootPermissionStatus.Unavailable;
        }

        private static bool IsRootAvailable(ContentResolver contentResolver, string rootKey)
        {
            try
            {
                AndroidUri? treeUri = AndroidUri.Parse(rootKey);
                if (treeUri is null)
                {
                    return false;
                }

                string? rootDocumentId = DocumentsContract.GetTreeDocumentId(treeUri);
                if (string.IsNullOrWhiteSpace(rootDocumentId))
                {
                    return false;
                }

                AndroidUri? rootUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, rootDocumentId);
                if (rootUri is null)
                {
                    return false;
                }

                using ICursor? cursor = contentResolver.Query(rootUri, RootProjection, null, null, null);
                return cursor is not null
                    && cursor.MoveToFirst()
                    && !cursor.IsNull(0);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return false;
            }
            catch (Java.Lang.SecurityException)
            {
                return false;
            }
            catch (Java.Lang.IllegalArgumentException)
            {
                return false;
            }
        }
    }
}
#endif
