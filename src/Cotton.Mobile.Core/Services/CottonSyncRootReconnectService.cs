// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootReconnectService(ICottonSyncRootStore rootStore)
    {
        private readonly ICottonSyncRootStore _rootStore =
            rootStore ?? throw new ArgumentNullException(nameof(rootStore));

        public async Task<CottonSyncRootSnapshot> ReconnectAsync(
            CottonSyncRootSnapshot root,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!root.NeedsUserAction)
            {
                throw new ArgumentException(
                    "Sync root does not need local source access.",
                    nameof(root));
            }

            if (!localRoot.CanReadWrite
                || localRoot.StorageKind != root.LocalRoot.StorageKind)
            {
                throw new ArgumentException(
                    "Replacement local source must have available access and match the configured source kind.",
                    nameof(localRoot));
            }

            if (!string.Equals(root.LocalRoot.RootKey, localRoot.RootKey, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Reconnect requires the originally configured local folder.",
                    nameof(localRoot));
            }

            CottonSyncRootSnapshot reconnectedRoot = new(
                root.Id,
                root.InstanceUri,
                root.AccountScopeKey,
                root.CloudFolder,
                localRoot,
                root.Direction,
                root.UploadOriginalRetention);
            await _rootStore
                .AddOrReplaceAsync(root.InstanceUri, reconnectedRoot, cancellationToken)
                .ConfigureAwait(false);
            return reconnectedRoot;
        }
    }
}
