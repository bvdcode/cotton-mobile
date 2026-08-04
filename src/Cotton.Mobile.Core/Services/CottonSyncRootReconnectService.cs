// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootReconnectService
    {
        private readonly ICottonSyncRootStore _rootStore;

        public CottonSyncRootReconnectService(ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(rootStore);

            _rootStore = rootStore;
        }

        public async Task<CottonSyncRootSnapshot> ReconnectUserSelectedDocumentTreeAsync(
            CottonSyncRootSnapshot root,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!root.LocalRoot.RequiresPersistedUserGrant || !root.NeedsUserAction)
            {
                throw new ArgumentException(
                    "Sync root does not need a user-selected folder grant.",
                    nameof(root));
            }

            if (!localRoot.RequiresPersistedUserGrant || !localRoot.CanReadWrite)
            {
                throw new ArgumentException(
                    "Replacement local root must have an available document tree grant.",
                    nameof(localRoot));
            }

            CottonSyncRootSnapshot reconnectedRoot = new(
                root.Id,
                root.InstanceUri,
                root.AccountScopeKey,
                root.CloudFolder,
                localRoot,
                root.Direction);
            await _rootStore
                .AddOrReplaceAsync(root.InstanceUri, reconnectedRoot, cancellationToken)
                .ConfigureAwait(false);
            return reconnectedRoot;
        }
    }
}
