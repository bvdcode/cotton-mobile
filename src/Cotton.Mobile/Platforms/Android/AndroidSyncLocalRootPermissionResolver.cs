// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidSyncLocalRootPermissionResolver :
        ICottonSyncLocalRootPermissionResolver
    {
        private static readonly string[] RootProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
        ];

        public CottonSyncRootPermissionStatus Resolve(CottonSyncLocalRootSnapshot localRoot)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            return localRoot.StorageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory => localRoot.PermissionStatus,
                CottonSyncRootStorageKind.UserSelectedDocumentTree => ResolveDocumentTree(localRoot),
                CottonSyncRootStorageKind.MediaStore => ResolveMediaStore(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(localRoot),
                    localRoot.StorageKind,
                    "Sync root storage kind is not supported."),
            };
        }

        private static CottonSyncRootPermissionStatus ResolveDocumentTree(CottonSyncLocalRootSnapshot localRoot)
        {
            ContentResolver contentResolver = global::Android.App.Application.Context.ContentResolver
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

        private static CottonSyncRootPermissionStatus ResolveMediaStore()
        {
            Context context = global::Android.App.Application.Context;
            bool isGranted = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? context.CheckSelfPermission(global::Android.Manifest.Permission.ReadMediaImages) == Permission.Granted
                    && context.CheckSelfPermission(global::Android.Manifest.Permission.ReadMediaVideo) == Permission.Granted
                : context.CheckSelfPermission(global::Android.Manifest.Permission.ReadExternalStorage) == Permission.Granted;
            return isGranted
                ? CottonSyncRootPermissionStatus.Available
                : CottonSyncRootPermissionStatus.Revoked;
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
