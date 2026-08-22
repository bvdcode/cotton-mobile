// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Globalization;
using Android.Content;
using Android.Database;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public partial class AndroidMediaStoreDeviceToCloudLocalTreeReader
    {
        private static void ReadCollection(
            ContentResolver resolver,
            AndroidMediaStoreCollectionKind collectionKind,
            AndroidMediaStoreScope scope,
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> items,
            List<CottonDeviceToCloudLocalProblemSnapshot> problems,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot>? revisions,
            AndroidMediaStoreScanStatistics statistics,
            CottonSyncScanProgressReporter progress,
            DateTime scanStartedAtUtc,
            CancellationToken cancellationToken)
        {
            AndroidUri collectionUri = GetCollectionUri(collectionKind);
            string[] projection = CreateProjection();
            long[] bucketIds = [.. scope.BucketIds.Order()];
            string placeholders = string.Join(",", bucketIds.Select(_ => "?"));
            string bucketColumn = AndroidMediaStoreColumnNames.GetBucketId(collectionKind);
            string bucketSelection = $"{bucketColumn} IN ({placeholders})";
            string selection = OperatingSystem.IsAndroidVersionAtLeast(29)
                ? $"{MediaStore.IMediaColumns.IsPending} = 0 AND {bucketSelection}"
                : bucketSelection;
            string[] selectionArguments = [.. bucketIds.Select(
                bucketId => bucketId.ToString(CultureInfo.InvariantCulture))];
            using ICursor cursor = resolver.Query(
                    collectionUri,
                    projection,
                    selection,
                    selectionArguments,
                    null)
                ?? throw new IOException("Could not read Android media collection.");

            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                long mediaId = cursor.GetLong(IdColumnIndex);
                string displayName = cursor.GetString(DisplayNameColumnIndex) ?? string.Empty;
                if (CottonSyncIgnoredFileName.IsIgnored(displayName))
                {
                    continue;
                }

                string? scopedStorageRelativePath = OperatingSystem.IsAndroidVersionAtLeast(29)
                    ? cursor.GetString(RelativePathColumnIndex)
                    : null;
                string rawParentPath = AndroidMediaStorePathMapper.CreateParentPath(
                    collectionKind,
                    mediaId,
                    scopedStorageRelativePath);
                string rawRelativePath = AndroidMediaStorePathMapper.CreateRawFilePath(
                    rawParentPath,
                    displayName);
                if (!AndroidMediaStorePathMapper.TryCreateFilePath(
                        rawParentPath,
                        displayName,
                        out string? relativePath))
                {
                    problems.Add(AndroidMediaStorePathMapper.CreateInvalidNameProblem(
                        displayName,
                        rawRelativePath));
                    continue;
                }

                AndroidMediaStorePathMapper.AddParentFolders(items, relativePath, scanStartedAtUtc);
                AndroidUri contentUri = ContentUris.WithAppendedId(collectionUri, mediaId)
                    ?? throw new IOException("Could not create Android media content URI.");
                string localSourceId = contentUri.ToString()
                    ?? throw new IOException("Could not create Android media source id.");
                string contentHash = ResolveContentHash(
                    resolver,
                    cursor,
                    contentUri,
                    localSourceId,
                    previousIndex,
                    revisions,
                    statistics,
                    cancellationToken);
                CottonDeviceToCloudLocalItemSnapshot file = CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                    displayName,
                    relativePath,
                    ReadLastModifiedUtc(cursor, scanStartedAtUtc),
                    ReadSizeBytes(cursor),
                    ReadOptionalString(cursor, MimeTypeColumnIndex),
                    localSourceId,
                    contentHash);
                if (!items.TryAdd(file.RelativePath, file))
                {
                    throw new IOException(
                        $"Android media collection contains a duplicate path: {file.RelativePath}.");
                }

                progress.RecordScannedItem();
            }
        }

        private static AndroidUri GetCollectionUri(AndroidMediaStoreCollectionKind collectionKind)
        {
            AndroidUri? collectionUri = collectionKind switch
            {
                AndroidMediaStoreCollectionKind.Images => MediaStore.Images.Media.ExternalContentUri,
                AndroidMediaStoreCollectionKind.Videos => MediaStore.Video.Media.ExternalContentUri,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(collectionKind),
                    collectionKind,
                    "Android media collection is not supported."),
            };
            return collectionUri
                ?? throw new InvalidOperationException("Android media collection URI is unavailable.");
        }

        private static DateTime ReadLastModifiedUtc(ICursor cursor, DateTime scanStartedAtUtc)
        {
            if (cursor.IsNull(DateModifiedColumnIndex))
            {
                return scanStartedAtUtc;
            }

            long seconds = cursor.GetLong(DateModifiedColumnIndex);
            return seconds <= 0
                ? scanStartedAtUtc
                : DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        private static long? ReadSizeBytes(ICursor cursor)
        {
            if (cursor.IsNull(SizeColumnIndex))
            {
                return null;
            }

            long sizeBytes = cursor.GetLong(SizeColumnIndex);
            return sizeBytes < 0 ? null : sizeBytes;
        }

        private static string? ReadOptionalString(ICursor cursor, int columnIndex)
        {
            return cursor.IsNull(columnIndex) ? null : cursor.GetString(columnIndex);
        }
    }
}
#endif
