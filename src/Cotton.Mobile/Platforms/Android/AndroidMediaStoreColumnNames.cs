// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Provider;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidMediaStoreColumnNames
    {
        public static string GetBucketId(AndroidMediaStoreCollectionKind collectionKind)
        {
            return collectionKind switch
            {
                AndroidMediaStoreCollectionKind.Images => MediaStore.Images.IImageColumns.BucketId,
                AndroidMediaStoreCollectionKind.Videos => MediaStore.Video.IVideoColumns.BucketId,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(collectionKind),
                    collectionKind,
                    "Android media collection is not supported."),
            };
        }

        public static string GetBucketDisplayName(AndroidMediaStoreCollectionKind collectionKind)
        {
            return collectionKind switch
            {
                AndroidMediaStoreCollectionKind.Images => MediaStore.Images.IImageColumns.BucketDisplayName,
                AndroidMediaStoreCollectionKind.Videos => MediaStore.Video.IVideoColumns.BucketDisplayName,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(collectionKind),
                    collectionKind,
                    "Android media collection is not supported."),
            };
        }
    }
}
#endif
