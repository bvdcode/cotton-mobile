// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaStoreAlbumProvider
    {
        private const int BucketIdColumnIndex = 0;
        private const int BucketDisplayNameColumnIndex = 1;

        public static Task<IReadOnlyList<CottonMediaAlbumSnapshot>> LoadAsync(
            AndroidMediaReadAccessSnapshot access,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(access);
            return Task.Run(() => Load(access, cancellationToken), cancellationToken);
        }

        private static IReadOnlyList<CottonMediaAlbumSnapshot> Load(
            AndroidMediaReadAccessSnapshot access,
            CancellationToken cancellationToken)
        {
            ContentResolver resolver = global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
            Dictionary<long, string> albumNames = [];
            Dictionary<long, int> albumItemCounts = [];
            if (access.CanReadImages)
            {
                ReadCollection(
                    resolver,
                    AndroidMediaStoreCollectionKind.Images,
                    albumNames,
                    albumItemCounts,
                    cancellationToken);
            }

            if (access.CanReadVideos)
            {
                ReadCollection(
                    resolver,
                    AndroidMediaStoreCollectionKind.Videos,
                    albumNames,
                    albumItemCounts,
                    cancellationToken);
            }

            return [.. albumNames
                .Select(album => new CottonMediaAlbumSnapshot(
                    album.Key,
                    album.Value,
                    albumItemCounts[album.Key]))
                .OrderBy(album => album.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(album => album.Id)];
        }

        private static void ReadCollection(
            ContentResolver resolver,
            AndroidMediaStoreCollectionKind collectionKind,
            Dictionary<long, string> albumNames,
            Dictionary<long, int> albumItemCounts,
            CancellationToken cancellationToken)
        {
            AndroidUri uri = GetCollectionUri(collectionKind);
            string[] projection =
            [
                AndroidMediaStoreColumnNames.GetBucketId(collectionKind),
                AndroidMediaStoreColumnNames.GetBucketDisplayName(collectionKind),
            ];
            string? selection = OperatingSystem.IsAndroidVersionAtLeast(29)
                ? $"{MediaStore.IMediaColumns.IsPending} = 0"
                : null;
            using ICursor cursor = resolver.Query(uri, projection, selection, null, null)
                ?? throw new IOException("Could not read Android media folders.");
            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                long bucketId = cursor.GetLong(BucketIdColumnIndex);
                string displayName = cursor.IsNull(BucketDisplayNameColumnIndex)
                    ? SyncRootSetupResources.UnnamedMediaAlbum
                    : cursor.GetString(BucketDisplayNameColumnIndex)?.Trim()
                        ?? SyncRootSetupResources.UnnamedMediaAlbum;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = SyncRootSetupResources.UnnamedMediaAlbum;
                }

                albumNames.TryAdd(bucketId, displayName);

                albumItemCounts.TryGetValue(bucketId, out int itemCount);
                albumItemCounts[bucketId] = itemCount + 1;
            }
        }

        private static AndroidUri GetCollectionUri(AndroidMediaStoreCollectionKind collectionKind)
        {
            AndroidUri? uri = collectionKind switch
            {
                AndroidMediaStoreCollectionKind.Images => MediaStore.Images.Media.ExternalContentUri,
                AndroidMediaStoreCollectionKind.Videos => MediaStore.Video.Media.ExternalContentUri,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(collectionKind),
                    collectionKind,
                    "Android media collection is not supported."),
            };
            return uri ?? throw new InvalidOperationException("Android media collection URI is unavailable.");
        }
    }
}
#endif
