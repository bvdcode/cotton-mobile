namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncRootSetupService
    {
        private readonly ICottonSyncRootStore _rootStore;

        public CottonBidirectionalSyncRootSetupService(ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(rootStore);

            _rootStore = rootStore;
        }

        public async Task<CottonBidirectionalSyncRootSetupResult> EnableUserSelectedDocumentTreeRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!localRoot.RequiresPersistedUserGrant)
            {
                throw new ArgumentException("Bidirectional sync roots require a document tree local root.", nameof(localRoot));
            }

            if (!localRoot.CanReadWrite)
            {
                throw new ArgumentException("Bidirectional sync roots require an available folder grant.", nameof(localRoot));
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
                && existingRoot.CanRunSync
                && existingRoot.Direction == CottonSyncDirection.Bidirectional)
            {
                return new CottonBidirectionalSyncRootSetupResult(
                    CottonBidirectionalSyncRootSetupStatus.AlreadyConfigured,
                    existingRoot);
            }

            CottonSyncRootSnapshot rootToSave = existingRoot is null
                ? candidate
                : CreateRoot(existingRoot.Id, instanceUri, accountScopeKey, cloudFolder, localRoot);
            await _rootStore.AddOrReplaceAsync(instanceUri, rootToSave, cancellationToken).ConfigureAwait(false);

            return new CottonBidirectionalSyncRootSetupResult(
                existingRoot is null
                    ? CottonBidirectionalSyncRootSetupStatus.Created
                    : CottonBidirectionalSyncRootSetupStatus.Updated,
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
                CottonSyncDirection.Bidirectional);
        }
    }
}
