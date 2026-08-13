// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public partial class AndroidMediaStoreDeviceToCloudLocalTreeReader(
        TimeProvider timeProvider,
        ICottonContentRevisionStore revisionStore) :
        ICottonDeviceToCloudLocalTreeReader
    {
        private const int IdColumnIndex = 0;
        private const int DisplayNameColumnIndex = 1;
        private const int DateModifiedColumnIndex = 2;
        private const int SizeColumnIndex = 3;
        private const int MimeTypeColumnIndex = 4;
        private const int RelativePathColumnIndex = 5;

        private static readonly string[] LegacyProjection =
        [
            IBaseColumns.Id,
            MediaStore.IMediaColumns.DisplayName,
            MediaStore.IMediaColumns.DateModified,
            MediaStore.IMediaColumns.Size,
            MediaStore.IMediaColumns.MimeType,
        ];

        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        private readonly ICottonContentRevisionStore _revisionStore =
            revisionStore ?? throw new ArgumentNullException(nameof(revisionStore));

        public async Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);

            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                AndroidMediaStoreScanResult legacyResult = await Task.Run(
                        () => ReadMedia(root, sourceVersion: null, previousIndex: null, cancellationToken),
                        cancellationToken)
                    .ConfigureAwait(false);
                return legacyResult.Content;
            }

            string sourceVersion = ReadSourceVersion();
            CottonContentRevisionIndexSnapshot? storedIndex = await _revisionStore
                .LoadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonContentRevisionIndexSnapshot? previousIndex =
                string.Equals(storedIndex?.SourceVersion, sourceVersion, StringComparison.Ordinal)
                    ? storedIndex
                    : null;
            AndroidMediaStoreScanResult result = await Task.Run(
                    () => ReadMedia(root, sourceVersion, previousIndex, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            CottonContentRevisionIndexSnapshot revisionIndex = result.RevisionIndex
                ?? throw new InvalidOperationException("Android MediaStore revision index was not produced.");
            if (!revisionIndex.HasSameContentAs(storedIndex))
            {
                await _revisionStore
                    .SaveAsync(instanceUri, root, revisionIndex, cancellationToken)
                    .ConfigureAwait(false);
            }

            return result.Content;
        }

        private AndroidMediaStoreScanResult ReadMedia(
            CottonSyncRootSnapshot root,
            string? sourceVersion,
            CottonContentRevisionIndexSnapshot? previousIndex,
            CancellationToken cancellationToken)
        {
            ContentResolver resolver = GetContentResolver();
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> items =
                new(StringComparer.OrdinalIgnoreCase);
            List<CottonDeviceToCloudLocalProblemSnapshot> problems = [];
            List<CottonContentRevisionSnapshot>? revisions = sourceVersion is null ? null : [];
            DateTime scanStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            ReadCollection(
                resolver,
                AndroidMediaStoreCollectionKind.Images,
                items,
                problems,
                previousIndex,
                revisions,
                scanStartedAtUtc,
                cancellationToken);
            ReadCollection(
                resolver,
                AndroidMediaStoreCollectionKind.Videos,
                items,
                problems,
                previousIndex,
                revisions,
                scanStartedAtUtc,
                cancellationToken);

            CottonDeviceToCloudLocalContentSnapshot content = new(
                root.LocalRoot.DisplayName,
                [.. items.Values],
                problems);
            CottonContentRevisionIndexSnapshot? revisionIndex = sourceVersion is null
                ? null
                : new CottonContentRevisionIndexSnapshot(sourceVersion, revisions!);
            return new AndroidMediaStoreScanResult(content, revisionIndex);
        }

        private static void ReadCollection(
            ContentResolver resolver,
            AndroidMediaStoreCollectionKind collectionKind,
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> items,
            List<CottonDeviceToCloudLocalProblemSnapshot> problems,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot>? revisions,
            DateTime scanStartedAtUtc,
            CancellationToken cancellationToken)
        {
            AndroidUri collectionUri = GetCollectionUri(collectionKind);
            string[] projection = CreateProjection();
            string? selection = OperatingSystem.IsAndroidVersionAtLeast(29)
                ? $"{MediaStore.IMediaColumns.IsPending} = 0"
                : null;
            using ICursor cursor = resolver.Query(collectionUri, projection, selection, null, null)
                ?? throw new IOException("Could not read Android media collection.");

            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                long mediaId = cursor.GetLong(IdColumnIndex);
                string displayName = cursor.GetString(DisplayNameColumnIndex) ?? string.Empty;
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

                AndroidMediaStorePathMapper.AddParentFolders(items, relativePath!, scanStartedAtUtc);
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
                    cancellationToken);
                CottonDeviceToCloudLocalItemSnapshot file = CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                    displayName,
                    relativePath!,
                    ReadLastModifiedUtc(cursor, scanStartedAtUtc),
                    ReadSizeBytes(cursor),
                    ReadOptionalString(cursor, MimeTypeColumnIndex),
                    localSourceId,
                    contentHash);
                if (!items.TryAdd(file.RelativePath, file))
                {
                    throw new IOException($"Android media collection contains a duplicate path: {file.RelativePath}.");
                }
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

        private static string ComputeContentHash(
            ContentResolver resolver,
            AndroidUri contentUri,
            CancellationToken cancellationToken)
        {
            using Stream content = resolver.OpenInputStream(contentUri)
                ?? throw new IOException("Could not open Android media content.");
            return CottonContentHash.ComputeSha256(content, cancellationToken);
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

            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!root.CanRunSync || !root.LocalRoot.UsesMediaStore)
            {
                throw new InvalidOperationException("Android media sync root is not ready.");
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("Android media sync requires device-to-cloud direction.");
            }
        }
    }
}
#endif
