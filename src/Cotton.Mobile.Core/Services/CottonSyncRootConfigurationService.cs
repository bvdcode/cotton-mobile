// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootConfigurationService
    {
        private readonly ICottonSyncRootStore _rootStore;

        public CottonSyncRootConfigurationService(ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(rootStore);

            _rootStore = rootStore;
        }

        public async Task<CottonSyncRootConfigurationResult> ConfigureUserSelectedDocumentTreeRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention uploadOriginalRetention,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!localRoot.RequiresPersistedUserGrant)
            {
                throw new ArgumentException("Sync roots require a document tree local root.", nameof(localRoot));
            }

            if (!localRoot.CanReadWrite)
            {
                throw new ArgumentException("Sync roots require an available folder grant.", nameof(localRoot));
            }

            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            ArgumentNullException.ThrowIfNull(cloudFolder);
            ValidateConfiguration(direction, uploadOriginalRetention);

            CottonSyncRootSnapshot candidate = CreateRoot(
                Guid.NewGuid(),
                instanceUri,
                accountScopeKey,
                cloudFolder,
                localRoot,
                direction,
                uploadOriginalRetention);
            IReadOnlyList<CottonSyncRootSnapshot> existingRoots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonSyncRootSnapshot? existingRoot = existingRoots
                .FirstOrDefault(root => string.Equals(root.StableKey, candidate.StableKey, StringComparison.Ordinal));
            CottonSyncRootSnapshot? localRootOwner = existingRoots.FirstOrDefault(
                root => HasSameAccountAndLocalRoot(root, candidate));

            if (existingRoot is null && localRootOwner is not null)
            {
                return new CottonSyncRootConfigurationResult(
                    CottonSyncRootConfigurationStatus.AlreadyConfigured,
                    localRootOwner);
            }

            if (existingRoot is not null
                && (existingRoot.Direction != direction
                    || HasSameEffectiveConfiguration(existingRoot, candidate)))
            {
                return new CottonSyncRootConfigurationResult(
                    CottonSyncRootConfigurationStatus.AlreadyConfigured,
                    existingRoot);
            }

            if (existingRoot is null)
            {
                await _rootStore.AddOrReplaceAsync(instanceUri, candidate, cancellationToken).ConfigureAwait(false);
                return new CottonSyncRootConfigurationResult(
                    CottonSyncRootConfigurationStatus.Created,
                    candidate);
            }

            CottonSyncRootSnapshot updatedRoot = CreateRoot(
                existingRoot.Id,
                instanceUri,
                accountScopeKey,
                cloudFolder,
                localRoot,
                direction,
                uploadOriginalRetention);
            await _rootStore.AddOrReplaceAsync(instanceUri, updatedRoot, cancellationToken).ConfigureAwait(false);
            return new CottonSyncRootConfigurationResult(
                CottonSyncRootConfigurationStatus.Updated,
                updatedRoot);
        }

        private static void ValidateConfiguration(
            CottonSyncDirection direction,
            CottonUploadOriginalRetention uploadOriginalRetention)
        {
            if (!Enum.IsDefined(direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }

            if (!Enum.IsDefined(uploadOriginalRetention))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uploadOriginalRetention),
                    "Upload original retention is not supported.");
            }

            switch (direction)
            {
                case CottonSyncDirection.DeviceToCloud:
                    return;

                case CottonSyncDirection.Bidirectional:
                    if (uploadOriginalRetention != CottonUploadOriginalRetention.KeepOriginals)
                    {
                        throw new ArgumentException(
                            "Bidirectional sync roots must keep local originals.",
                            nameof(uploadOriginalRetention));
                    }

                    return;

                case CottonSyncDirection.CloudToDevice:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        "Cloud-to-device roots are not configured from a user-selected document tree.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }
        }

        private static bool HasSameEffectiveConfiguration(
            CottonSyncRootSnapshot existingRoot,
            CottonSyncRootSnapshot candidate)
        {
            return existingRoot.Direction == candidate.Direction
                && existingRoot.UploadOriginalRetention == candidate.UploadOriginalRetention
                && existingRoot.CloudFolder.FolderId == candidate.CloudFolder.FolderId
                && string.Equals(
                    existingRoot.CloudFolder.FolderName,
                    candidate.CloudFolder.FolderName,
                    StringComparison.Ordinal)
                && string.Equals(existingRoot.CloudFolder.Path, candidate.CloudFolder.Path, StringComparison.Ordinal)
                && existingRoot.LocalRoot.StorageKind == candidate.LocalRoot.StorageKind
                && string.Equals(existingRoot.LocalRoot.RootKey, candidate.LocalRoot.RootKey, StringComparison.Ordinal)
                && string.Equals(
                    existingRoot.LocalRoot.DisplayName,
                    candidate.LocalRoot.DisplayName,
                    StringComparison.Ordinal)
                && existingRoot.LocalRoot.PermissionStatus == candidate.LocalRoot.PermissionStatus;
        }

        private static bool HasSameAccountAndLocalRoot(
            CottonSyncRootSnapshot existingRoot,
            CottonSyncRootSnapshot candidate)
        {
            return string.Equals(
                    existingRoot.AccountScopeKey,
                    candidate.AccountScopeKey,
                    StringComparison.Ordinal)
                && existingRoot.LocalRoot.StorageKind == candidate.LocalRoot.StorageKind
                && string.Equals(
                    existingRoot.LocalRoot.RootKey,
                    candidate.LocalRoot.RootKey,
                    StringComparison.Ordinal);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid id,
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction,
            CottonUploadOriginalRetention uploadOriginalRetention)
        {
            return new CottonSyncRootSnapshot(
                id,
                instanceUri,
                accountScopeKey,
                cloudFolder,
                localRoot,
                direction,
                uploadOriginalRetention);
        }
    }
}
