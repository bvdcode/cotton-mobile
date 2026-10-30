// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonContentRevisionStore(
        ICottonContentRevisionPathProvider pathProvider) :
        ICottonContentRevisionStore,
        IDisposable
    {
        private const int SchemaVersion = 1;

        private readonly ICottonContentRevisionPathProvider _pathProvider =
            pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public async Task<CottonContentRevisionIndexSnapshot?> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            EnsureMatchingRoot(instanceUri, root);

            string filePath = _pathProvider.CreateContentRevisionFilePath(instanceUri, root);
            if (!File.Exists(filePath))
            {
                return null;
            }

            CottonStoredContentRevisionIndex? stored = await CottonAtomicJsonFile
                .ReadAsync<CottonStoredContentRevisionIndex>(filePath, cancellationToken)
                .ConfigureAwait(false);
            if (stored is null
                || stored.SchemaVersion != SchemaVersion
                || !string.Equals(stored.SyncRootStableKey, root.StableKey, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(stored.SourceVersion)
                || stored.Revisions is null)
            {
                throw new InvalidDataException("Content revision index is invalid for this sync root.");
            }

            try
            {
                return new CottonContentRevisionIndexSnapshot(
                    stored.SourceVersion,
                    stored.Revisions.Select(CreateRevision));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                throw new InvalidDataException("Content revision index contains invalid data.", exception);
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonContentRevisionIndexSnapshot index,
            CancellationToken cancellationToken = default)
        {
            EnsureMatchingRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(index);

            CottonStoredContentRevisionIndex stored = new()
            {
                SchemaVersion = SchemaVersion,
                SyncRootStableKey = root.StableKey,
                SourceVersion = index.SourceVersion,
                Revisions = [.. index.Revisions.Select<CottonContentRevisionSnapshot, CottonStoredContentRevision?>(
                    CreateStoredRevision)],
            };
            string filePath = _pathProvider.CreateContentRevisionFilePath(instanceUri, root);
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await CottonAtomicJsonFile.WriteAsync(filePath, stored, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            EnsureMatchingRoot(instanceUri, root);

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CottonAtomicJsonFile.DeleteIfExists(
                    _pathProvider.CreateContentRevisionFilePath(instanceUri, root));
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            _writeLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private static CottonContentRevisionSnapshot CreateRevision(CottonStoredContentRevision? stored)
        {
            if (stored is null)
            {
                throw new InvalidDataException("Content revision index contains an empty revision.");
            }

            return new CottonContentRevisionSnapshot(
                stored.LocalSourceId ?? string.Empty,
                stored.Generation,
                stored.ContentHash ?? string.Empty);
        }

        private static CottonStoredContentRevision CreateStoredRevision(CottonContentRevisionSnapshot revision)
        {
            return new CottonStoredContentRevision
            {
                LocalSourceId = revision.LocalSourceId,
                Generation = revision.Generation,
                ContentHash = revision.ContentHash,
            };
        }

        private static void EnsureMatchingRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new ArgumentException("Content revision instance does not match the sync root.", nameof(instanceUri));
            }
        }
    }
}
