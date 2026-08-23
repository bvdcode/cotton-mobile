// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Runtime.Versioning;
using Android.Content;
using Android.Database;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public partial class AndroidMediaStoreDeviceToCloudLocalTreeReader
    {
        private const string LegacyRevisionSourceVersion = "android-media-store-date-modified-v1";
        private const int GenerationModifiedColumnIndex = 6;

        private static string[] CreateProjection()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                return CreateRevisionProjection();
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                return CreateScopedStorageProjection();
            }

            return LegacyProjection;
        }

        [SupportedOSPlatform("android30.0")]
        private static string[] CreateRevisionProjection()
        {
            return
            [
                .. CreateScopedStorageProjection(),
                MediaStore.IMediaColumns.GenerationModified,
            ];
        }

        [SupportedOSPlatform("android29.0")]
        private static string[] CreateScopedStorageProjection()
        {
            return
            [
                .. LegacyProjection,
                MediaStore.IMediaColumns.RelativePath,
            ];
        }

        private static string ResolveContentHash(
            ContentResolver resolver,
            AndroidUri contentUri,
            string localSourceId,
            long? revision,
            long? sizeBytes,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot> revisions,
            AndroidMediaStoreScanStatistics statistics,
            CancellationToken cancellationToken)
        {
            if (revision.HasValue
                && sizeBytes.HasValue
                && previousIndex is not null
                && previousIndex.TryGetContentHash(
                    localSourceId,
                    revision.Value,
                    sizeBytes.Value,
                    out string? cachedHash))
            {
                statistics.RecordReusedHash();
                revisions.Add(new CottonContentRevisionSnapshot(
                    localSourceId,
                    revision.Value,
                    cachedHash,
                    sizeBytes.Value));
                return cachedHash;
            }

            statistics.RecordHashedFile();
            string contentHash = ComputeContentHash(resolver, contentUri, cancellationToken);
            if (revision.HasValue && sizeBytes.HasValue)
            {
                revisions.Add(new CottonContentRevisionSnapshot(
                    localSourceId,
                    revision.Value,
                    contentHash,
                    sizeBytes.Value));
            }

            return contentHash;
        }

        private static long? ReadRevision(ICursor cursor)
        {
            return OperatingSystem.IsAndroidVersionAtLeast(30)
                ? ReadGeneration(cursor)
                : ReadDateModified(cursor);
        }

        private static long? ReadDateModified(ICursor cursor)
        {
            if (cursor.IsNull(DateModifiedColumnIndex))
            {
                return null;
            }

            long seconds = cursor.GetLong(DateModifiedColumnIndex);
            return seconds <= 0 ? null : seconds;
        }

        private static long? ReadGeneration(ICursor cursor)
        {
            if (cursor.IsNull(GenerationModifiedColumnIndex))
            {
                return null;
            }

            long generation = cursor.GetLong(GenerationModifiedColumnIndex);
            return generation < 0 ? null : generation;
        }

        private static string CreateSourceVersion()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                return LegacyRevisionSourceVersion;
            }

            string? sourceVersion = MediaStore.GetVersion(
                global::Android.App.Application.Context,
                MediaStore.VolumeExternal);
            return sourceVersion
                ?? throw new InvalidOperationException("Android MediaStore version is unavailable.");
        }
    }
}
#endif
