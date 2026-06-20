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

            CottonSyncRootSnapshot candidate = CreateRoot(Guid.NewGuid(), instanceUri, accountScopeKey, cloudFolder);
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
                : CreateRoot(existingRoot.Id, instanceUri, accountScopeKey, cloudFolder);
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
            CottonUploadDestinationSnapshot cloudFolder)
        {
            return new CottonSyncRootSnapshot(
                id,
                instanceUri,
                accountScopeKey,
                cloudFolder,
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    AppPrivateCloudToDeviceRootKey,
                    AppPrivateCloudToDeviceDisplayName,
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);
        }
    }
}
