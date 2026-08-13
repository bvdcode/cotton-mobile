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
            ICursor cursor,
            AndroidUri contentUri,
            string localSourceId,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot>? revisions,
            AndroidMediaStoreScanStatistics statistics,
            CancellationToken cancellationToken)
        {
            if (revisions is null)
            {
                statistics.RecordHashedFile();
                return ComputeContentHash(resolver, contentUri, cancellationToken);
            }

            long? generation = ReadGeneration(cursor);
            if (generation.HasValue
                && previousIndex is not null
                && previousIndex.TryGetContentHash(localSourceId, generation.Value, out string? cachedHash))
            {
                statistics.RecordReusedHash();
                revisions.Add(new CottonContentRevisionSnapshot(localSourceId, generation.Value, cachedHash!));
                return cachedHash!;
            }

            statistics.RecordHashedFile();
            string contentHash = ComputeContentHash(resolver, contentUri, cancellationToken);
            if (generation.HasValue)
            {
                revisions.Add(new CottonContentRevisionSnapshot(localSourceId, generation.Value, contentHash));
            }

            return contentHash;
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

        [SupportedOSPlatform("android30.0")]
        private static string ReadSourceVersion()
        {
            string? sourceVersion = MediaStore.GetVersion(
                global::Android.App.Application.Context,
                MediaStore.VolumeExternal);
            return sourceVersion
                ?? throw new InvalidOperationException("Android MediaStore version is unavailable.");
        }
    }
}
#endif
