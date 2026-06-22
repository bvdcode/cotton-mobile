// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncRootSetupService
    {
        private const string AppPrivateCloudToDeviceRootKey = "app-private-cloud-to-device";
        private const string AppPrivateCloudToDeviceDisplayName = "On this device";

        private readonly ICottonSyncRootStore _rootStore;

        public CottonCloudToDeviceSyncRootSetupService(ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(rootStore);

            _rootStore = rootStore;
        }

        public async Task<CottonCloudToDeviceSyncRootSetupResult> EnableAppPrivateRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            ArgumentNullException.ThrowIfNull(cloudFolder);

            return await EnableRootAsync(
                    instanceUri,
                    accountScopeKey,
                    cloudFolder,
                    CreateAppPrivateLocalRoot(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CottonCloudToDeviceSyncRootSetupResult> EnableUserSelectedDocumentTreeRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!localRoot.RequiresPersistedUserGrant)
            {
                throw new ArgumentException("User-selected sync roots require a document tree local root.", nameof(localRoot));
            }

            if (!localRoot.CanReadWrite)
            {
                throw new ArgumentException("User-selected sync roots require an available folder grant.", nameof(localRoot));
            }

            return await EnableRootAsync(
                    instanceUri,
                    accountScopeKey,
                    cloudFolder,
                    localRoot,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<CottonCloudToDeviceSyncRootSetupResult> EnableRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            ArgumentNullException.ThrowIfNull(cloudFolder);
            ArgumentNullException.ThrowIfNull(localRoot);

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

            if (existingRoot is not null && existingRoot.CanRunSync && existingRoot.Direction == CottonSyncDirection.CloudToDevice)
            {
                return new CottonCloudToDeviceSyncRootSetupResult(
                    CottonCloudToDeviceSyncRootSetupStatus.AlreadyConfigured,
                    existingRoot);
            }

            CottonSyncRootSnapshot rootToSave = existingRoot is null
                ? candidate
                : CreateRoot(existingRoot.Id, instanceUri, accountScopeKey, cloudFolder, localRoot);
            await _rootStore.AddOrReplaceAsync(instanceUri, rootToSave, cancellationToken).ConfigureAwait(false);

            return new CottonCloudToDeviceSyncRootSetupResult(
                existingRoot is null
                    ? CottonCloudToDeviceSyncRootSetupStatus.Created
                    : CottonCloudToDeviceSyncRootSetupStatus.Updated,
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
                CottonSyncDirection.CloudToDevice);
        }

        private static CottonSyncLocalRootSnapshot CreateAppPrivateLocalRoot()
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                AppPrivateCloudToDeviceRootKey,
                AppPrivateCloudToDeviceDisplayName,
                CottonSyncRootPermissionStatus.Available);
        }
    }
}
