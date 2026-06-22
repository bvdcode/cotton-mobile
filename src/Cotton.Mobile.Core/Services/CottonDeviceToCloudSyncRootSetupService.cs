// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncRootSetupService
    {
        private readonly ICottonSyncRootStore _rootStore;

        public CottonDeviceToCloudSyncRootSetupService(ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(rootStore);

            _rootStore = rootStore;
        }

        public async Task<CottonDeviceToCloudSyncRootSetupResult> EnableUserSelectedDocumentTreeRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!localRoot.RequiresPersistedUserGrant)
            {
                throw new ArgumentException("Device-to-cloud sync roots require a document tree local root.", nameof(localRoot));
            }

            if (!localRoot.CanReadWrite)
            {
                throw new ArgumentException("Device-to-cloud sync roots require an available folder grant.", nameof(localRoot));
            }

            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            ArgumentNullException.ThrowIfNull(cloudFolder);

            CottonSyncRootSnapshot candidate = CreateRoot(
                Guid.NewGuid(),
                instanceUri,
                accountScopeKey,
                cloudFolder,
                localRoot);
            IReadOnlyList<CottonSyncRootSnapshot> existingRoots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonSyncRootSnapshot? existingRoot = existingRoots
                .FirstOrDefault(root => string.Equals(root.StableKey, candidate.StableKey, StringComparison.Ordinal));

            if (existingRoot is not null
                && existingRoot.Direction is not CottonSyncDirection.DeviceToCloud
                && existingRoot.Direction is not CottonSyncDirection.Bidirectional)
            {
                return new CottonDeviceToCloudSyncRootSetupResult(
                    CottonDeviceToCloudSyncRootSetupStatus.DirectionConflict,
                    existingRoot);
            }

            if (existingRoot is not null && existingRoot.CanRunSync)
            {
                return new CottonDeviceToCloudSyncRootSetupResult(
                    CottonDeviceToCloudSyncRootSetupStatus.AlreadyConfigured,
                    existingRoot);
            }

            CottonSyncRootSnapshot rootToSave = existingRoot is null
                ? candidate
                : CreateRoot(existingRoot.Id, instanceUri, accountScopeKey, cloudFolder, localRoot);
            await _rootStore.AddOrReplaceAsync(instanceUri, rootToSave, cancellationToken).ConfigureAwait(false);

            return new CottonDeviceToCloudSyncRootSetupResult(
                existingRoot is null
                    ? CottonDeviceToCloudSyncRootSetupStatus.Created
                    : CottonDeviceToCloudSyncRootSetupStatus.Updated,
                rootToSave);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid id,
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot)
        {
            return new CottonSyncRootSnapshot(
                id,
                instanceUri,
                accountScopeKey,
                cloudFolder,
                localRoot,
                CottonSyncDirection.DeviceToCloud);
        }
    }
}
