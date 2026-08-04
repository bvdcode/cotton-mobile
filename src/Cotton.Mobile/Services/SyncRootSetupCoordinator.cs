// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupCoordinator
    {
        private readonly ICloudFolderPickerService _cloudFolderPicker;
        private readonly ICottonSyncLocalRootPickerService _localRootPicker;
        private readonly CottonBidirectionalSyncRootSetupService _setupService;
        private readonly CottonSyncRootReconnectService _reconnectService;

        public SyncRootSetupCoordinator(
            ICloudFolderPickerService cloudFolderPicker,
            ICottonSyncLocalRootPickerService localRootPicker,
            CottonBidirectionalSyncRootSetupService setupService,
            CottonSyncRootReconnectService reconnectService)
        {
            ArgumentNullException.ThrowIfNull(cloudFolderPicker);
            ArgumentNullException.ThrowIfNull(localRootPicker);
            ArgumentNullException.ThrowIfNull(setupService);
            ArgumentNullException.ThrowIfNull(reconnectService);

            _cloudFolderPicker = cloudFolderPicker;
            _localRootPicker = localRootPicker;
            _setupService = setupService;
            _reconnectService = reconnectService;
        }

        public async Task<SyncRootSetupResult> AddBidirectionalRootAsync(
            Uri instanceUri,
            string accountScopeKey,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            if (!_localRootPicker.IsAvailable)
            {
                return new SyncRootSetupResult(
                    SyncRootSetupStatus.Unavailable,
                    "Folder sync is not available on this device.");
            }

            CottonUploadDestinationSnapshot? cloudFolder = await _cloudFolderPicker
                .PickAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            if (cloudFolder is null)
            {
                return Cancelled();
            }

            CottonSyncLocalRootSnapshot? localRoot = await _localRootPicker
                .PickUserSelectedDocumentTreeAsync(cancellationToken)
                .ConfigureAwait(false);
            if (localRoot is null)
            {
                return Cancelled();
            }

            CottonBidirectionalSyncRootSetupResult result = await _setupService
                .EnableUserSelectedDocumentTreeRootAsync(
                    instanceUri,
                    accountScopeKey,
                    cloudFolder,
                    localRoot,
                    cancellationToken)
                .ConfigureAwait(false);
            return result.Status switch
            {
                CottonBidirectionalSyncRootSetupStatus.Created => new SyncRootSetupResult(
                    SyncRootSetupStatus.Created,
                    $"Syncing {cloudFolder.Path}."),
                CottonBidirectionalSyncRootSetupStatus.Updated => new SyncRootSetupResult(
                    SyncRootSetupStatus.Updated,
                    $"Updated sync for {cloudFolder.Path}."),
                CottonBidirectionalSyncRootSetupStatus.AlreadyConfigured => new SyncRootSetupResult(
                    SyncRootSetupStatus.AlreadyConfigured,
                    $"{cloudFolder.Path} is already syncing."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(result),
                    "Sync root setup status is not supported."),
            };
        }

        public async Task<SyncRootSetupResult> ReconnectLocalRootAsync(
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);

            if (!_localRootPicker.IsAvailable)
            {
                return new SyncRootSetupResult(
                    SyncRootSetupStatus.Unavailable,
                    "Folder sync is not available on this device.");
            }

            CottonSyncLocalRootSnapshot? localRoot = await _localRootPicker
                .PickUserSelectedDocumentTreeAsync(cancellationToken)
                .ConfigureAwait(false);
            if (localRoot is null)
            {
                return Cancelled();
            }

            CottonSyncRootSnapshot reconnectedRoot = await _reconnectService
                .ReconnectUserSelectedDocumentTreeAsync(root, localRoot, cancellationToken)
                .ConfigureAwait(false);
            return new SyncRootSetupResult(
                SyncRootSetupStatus.Updated,
                $"Reconnected {reconnectedRoot.CloudFolder.Path}.");
        }

        private static SyncRootSetupResult Cancelled()
        {
            return new SyncRootSetupResult(SyncRootSetupStatus.Cancelled, string.Empty);
        }
    }
}
