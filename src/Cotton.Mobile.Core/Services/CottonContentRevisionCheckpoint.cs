// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonContentRevisionCheckpoint
    {
        public const int DefaultItemInterval = 64;

        private readonly ICottonContentRevisionStore _store;
        private readonly Uri _instanceUri;
        private readonly CottonSyncRootSnapshot _root;
        private readonly string _sourceVersion;
        private readonly int _itemInterval;
        private CottonContentRevisionIndexSnapshot? _persistedIndex;
        private int _lastCheckpointItemCount;

        public CottonContentRevisionCheckpoint(
            ICottonContentRevisionStore store,
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            string sourceVersion,
            CottonContentRevisionIndexSnapshot? persistedIndex,
            int itemInterval = DefaultItemInterval)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceVersion);
            ArgumentOutOfRangeException.ThrowIfLessThan(itemInterval, 1);
            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new ArgumentException("Content revision instance does not match the sync root.", nameof(root));
            }

            string normalizedSourceVersion = sourceVersion.Trim();
            if (persistedIndex is not null
                && !string.Equals(
                    persistedIndex.SourceVersion,
                    normalizedSourceVersion,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Persisted content revisions use a different source version.",
                    nameof(persistedIndex));
            }

            _store = store;
            _instanceUri = instanceUri;
            _root = root;
            _sourceVersion = normalizedSourceVersion;
            _persistedIndex = persistedIndex;
            _itemInterval = itemInterval;
        }

        public async Task SaveProgressIfDueAsync(
            IReadOnlyCollection<CottonContentRevisionSnapshot> currentRevisions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(currentRevisions);
            if (currentRevisions.Count - _lastCheckpointItemCount < _itemInterval)
            {
                return;
            }

            CottonContentRevisionIndexSnapshot progressIndex = CreateProgressIndex(currentRevisions);
            await SaveIfChangedAsync(progressIndex, cancellationToken).ConfigureAwait(false);
            _lastCheckpointItemCount = currentRevisions.Count;
        }

        public Task SaveFinalAsync(
            IReadOnlyCollection<CottonContentRevisionSnapshot> currentRevisions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(currentRevisions);
            CottonContentRevisionIndexSnapshot finalIndex = new(_sourceVersion, currentRevisions);
            return SaveIfChangedAsync(finalIndex, cancellationToken);
        }

        private CottonContentRevisionIndexSnapshot CreateProgressIndex(
            IReadOnlyCollection<CottonContentRevisionSnapshot> currentRevisions)
        {
            if (_persistedIndex is null)
            {
                return new CottonContentRevisionIndexSnapshot(_sourceVersion, currentRevisions);
            }

            HashSet<string> currentSourceIds = currentRevisions
                .Select(revision => revision.LocalSourceId)
                .ToHashSet(StringComparer.Ordinal);
            List<CottonContentRevisionSnapshot> merged = [.. currentRevisions];
            merged.AddRange(_persistedIndex.Revisions.Where(
                revision => !currentSourceIds.Contains(revision.LocalSourceId)));
            return new CottonContentRevisionIndexSnapshot(_sourceVersion, merged);
        }

        private async Task SaveIfChangedAsync(
            CottonContentRevisionIndexSnapshot index,
            CancellationToken cancellationToken)
        {
            if (index.HasSameContentAs(_persistedIndex))
            {
                return;
            }

            await _store
                .SaveAsync(_instanceUri, _root, index, cancellationToken)
                .ConfigureAwait(false);
            _persistedIndex = index;
        }
    }
}
