// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
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
            AndroidMediaStoreScanResult result = await ReadWithDiagnosticsAsync(
                    instanceUri,
                    root,
                    cancellationToken)
                .ConfigureAwait(false);
            return result.Content;
        }

        internal async Task<AndroidMediaStoreScanResult> ReadWithDiagnosticsAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            if (!AndroidMediaStoreScopeKey.TryParse(root.LocalRoot.ScopeKey, out AndroidMediaStoreScope? scope))
            {
                throw new InvalidOperationException("Android media sync root does not have a selected scope.");
            }

            AndroidMediaStoreScope selectedScope = scope
                ?? throw new InvalidOperationException("Android media sync root scope is unavailable.");
            AndroidMediaReadAccessSnapshot access = AndroidMediaReadAccessResolver.Resolve();
            if (!access.HasAccess)
            {
                throw new UnauthorizedAccessException("Android media access is not available.");
            }

            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                AndroidMediaStoreScanResult legacyResult = await Task.Run(
                        () => ReadMedia(
                            root,
                            selectedScope,
                            access,
                            sourceVersion: null,
                            previousIndex: null,
                            cancellationToken),
                    cancellationToken)
                    .ConfigureAwait(false);
                return legacyResult;
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
                    () => ReadMedia(root, selectedScope, access, sourceVersion, previousIndex, cancellationToken),
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

            return result;
        }

        private AndroidMediaStoreScanResult ReadMedia(
            CottonSyncRootSnapshot root,
            AndroidMediaStoreScope scope,
            AndroidMediaReadAccessSnapshot access,
            string? sourceVersion,
            CottonContentRevisionIndexSnapshot? previousIndex,
            CancellationToken cancellationToken)
        {
            ContentResolver resolver = GetContentResolver();
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> items =
                new(StringComparer.OrdinalIgnoreCase);
            List<CottonDeviceToCloudLocalProblemSnapshot> problems = [];
            List<CottonContentRevisionSnapshot>? revisions = sourceVersion is null ? null : [];
            AndroidMediaStoreScanStatistics statistics = new();
            DateTime scanStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            if (access.CanReadImages)
            {
                ReadCollection(
                    resolver,
                    AndroidMediaStoreCollectionKind.Images,
                    scope,
                    items,
                    problems,
                    previousIndex,
                    revisions,
                    statistics,
                    scanStartedAtUtc,
                    cancellationToken);
            }

            if (access.CanReadVideos)
            {
                ReadCollection(
                    resolver,
                    AndroidMediaStoreCollectionKind.Videos,
                    scope,
                    items,
                    problems,
                    previousIndex,
                    revisions,
                    statistics,
                    scanStartedAtUtc,
                    cancellationToken);
            }

            CottonDeviceToCloudLocalContentSnapshot content = new(
                root.LocalRoot.DisplayName,
                [.. items.Values],
                problems);
            CottonContentRevisionIndexSnapshot? revisionIndex = CreateRevisionIndex(
                sourceVersion,
                revisions);
            return new AndroidMediaStoreScanResult(content, revisionIndex, statistics);
        }

        private static CottonContentRevisionIndexSnapshot? CreateRevisionIndex(
            string? sourceVersion,
            List<CottonContentRevisionSnapshot>? revisions)
        {
            if (sourceVersion is null)
            {
                return null;
            }

            return new CottonContentRevisionIndexSnapshot(
                sourceVersion,
                revisions ?? throw new InvalidOperationException("Android media revisions are unavailable."));
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
