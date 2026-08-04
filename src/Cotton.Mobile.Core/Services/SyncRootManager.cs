// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncRootManager
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly ICottonSyncLocalRootPermissionResolver _permissionResolver;

        public SyncRootManager(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonSyncLocalRootPermissionResolver permissionResolver)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(permissionResolver);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _permissionResolver = permissionResolver;
        }

        public async Task<SyncRootCollectionSnapshot> LoadAsync(
            Uri instanceUri,
            string accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            IReadOnlyList<CottonSyncRootSnapshot> storedRoots = await _rootStore.LoadAsync(instanceUri);
            IReadOnlyList<CottonSyncRootSnapshot> accountRoots = storedRoots
                .Where(root => string.Equals(
                    root.AccountScopeKey,
                    accountScopeKey,
                    StringComparison.Ordinal))
                .Select(ResolvePermission)
                .ToList();
            IReadOnlySet<Guid> pausedRootIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
            return new SyncRootCollectionSnapshot(accountRoots, pausedRootIds);
        }

        public async Task<bool> StopAsync(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            bool removed = await _rootStore.RemoveAsync(instanceUri, root.Id);
            await _pauseStore.SetPausedAsync(instanceUri, root.Id, isPaused: false);
            await _manifestStore.ClearAsync(instanceUri, root);
            return removed;
        }

        public async Task SetPausedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            bool isPaused)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            await _pauseStore.SetPausedAsync(instanceUri, root.Id, isPaused);
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
                permissionStatus);
            return new CottonSyncRootSnapshot(
                root.Id,
                root.InstanceUri,
                root.AccountScopeKey,
                root.CloudFolder,
                localRoot,
                root.Direction);
        }
    }
}
