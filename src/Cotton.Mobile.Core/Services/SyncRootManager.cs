// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncRootManager
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonContentRevisionStore _contentRevisionStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly ICottonAutomaticSyncStatusStore _automaticSyncStatusStore;
        private readonly ICottonSyncLocalRootPermissionResolver _permissionResolver;

        public SyncRootManager(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonContentRevisionStore contentRevisionStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonUploadReceiptStore uploadReceiptStore,
            ICottonAutomaticSyncStatusStore automaticSyncStatusStore,
            ICottonSyncLocalRootPermissionResolver permissionResolver)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(contentRevisionStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);
            ArgumentNullException.ThrowIfNull(automaticSyncStatusStore);
            ArgumentNullException.ThrowIfNull(permissionResolver);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _contentRevisionStore = contentRevisionStore;
            _manifestStore = manifestStore;
            _uploadReceiptStore = uploadReceiptStore;
            _automaticSyncStatusStore = automaticSyncStatusStore;
            _permissionResolver = permissionResolver;
        }

        public async Task<SyncRootCollectionSnapshot> LoadAsync(
            Uri instanceUri,
            string accountScopeKey,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            Task<IReadOnlyList<CottonSyncRootSnapshot>> rootsTask =
                _rootStore.LoadAsync(instanceUri, cancellationToken);
            Task<IReadOnlySet<Guid>> pausedRootsTask =
                _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken);
            Task<IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>> statusesTask =
                _automaticSyncStatusStore.LoadAsync(instanceUri, cancellationToken);
            await Task.WhenAll(rootsTask, pausedRootsTask, statusesTask).ConfigureAwait(false);

            IReadOnlyList<CottonSyncRootSnapshot> storedRoots = await rootsTask.ConfigureAwait(false);
            IReadOnlyList<CottonSyncRootSnapshot> accountRoots = [.. storedRoots
                .Where(root => string.Equals(
                    root.AccountScopeKey,
                    accountScopeKey,
                    StringComparison.Ordinal))
                .Select(ResolvePermission)];
            IReadOnlySet<Guid> pausedRootIds = await pausedRootsTask.ConfigureAwait(false);
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> storedStatuses =
                await statusesTask.ConfigureAwait(false);
            HashSet<Guid> accountRootIds = [.. accountRoots.Select(root => root.Id)];
            Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> accountStatuses = storedStatuses
                .Where(item => accountRootIds.Contains(item.Key))
                .ToDictionary();
            return new SyncRootCollectionSnapshot(accountRoots, pausedRootIds, accountStatuses);
        }

        public async Task<bool> DeleteAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            bool removed = await _rootStore
                .RemoveAsync(instanceUri, root.Id, cancellationToken)
                .ConfigureAwait(false);
            await _pauseStore
                .SetPausedAsync(instanceUri, root.Id, isPaused: false, cancellationToken)
                .ConfigureAwait(false);
            await _contentRevisionStore.ClearAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            await _manifestStore.ClearAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            await _uploadReceiptStore.ClearAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);

            return removed;
        }

        public async Task SetPausedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            bool isPaused,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            await _pauseStore
                .SetPausedAsync(instanceUri, root.Id, isPaused, cancellationToken)
                .ConfigureAwait(false);
        }

        private CottonSyncRootSnapshot ResolvePermission(CottonSyncRootSnapshot root)
        {
            CottonSyncRootPermissionStatus permissionStatus = _permissionResolver.Resolve(root.LocalRoot);
            if (permissionStatus == root.LocalRoot.PermissionStatus)
            {
                return root;
            }

            CottonSyncLocalRootSnapshot localRoot = new(
                root.LocalRoot.StorageKind,
                root.LocalRoot.RootKey,
                root.LocalRoot.DisplayName,
                permissionStatus,
                root.LocalRoot.ScopeKey);
            return new CottonSyncRootSnapshot(
                root.Id,
                root.InstanceUri,
                root.AccountScopeKey,
                root.CloudFolder,
                localRoot,
                root.Direction,
                root.UploadOriginalRetention);
        }
    }
}
