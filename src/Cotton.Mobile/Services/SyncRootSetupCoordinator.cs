// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupCoordinator
    {
        private readonly ISyncRootSetupOptionsPickerService _optionsPicker;
        private readonly ICloudFolderPickerService _cloudFolderPicker;
        private readonly ICottonSyncLocalRootPickerService _localRootPicker;
        private readonly CottonSyncRootConfigurationService _configurationService;
        private readonly CottonSyncRootReconnectService _reconnectService;

        public SyncRootSetupCoordinator(
            ISyncRootSetupOptionsPickerService optionsPicker,
            ICloudFolderPickerService cloudFolderPicker,
            ICottonSyncLocalRootPickerService localRootPicker,
            CottonSyncRootConfigurationService configurationService,
            CottonSyncRootReconnectService reconnectService)
        {
            ArgumentNullException.ThrowIfNull(optionsPicker);
            ArgumentNullException.ThrowIfNull(cloudFolderPicker);
            ArgumentNullException.ThrowIfNull(localRootPicker);
            ArgumentNullException.ThrowIfNull(configurationService);
            ArgumentNullException.ThrowIfNull(reconnectService);

            _optionsPicker = optionsPicker;
            _cloudFolderPicker = cloudFolderPicker;
            _localRootPicker = localRootPicker;
            _configurationService = configurationService;
            _reconnectService = reconnectService;
        }

        public async Task<SyncRootSetupResult> AddRootAsync(
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
                    SyncRootSetupResources.UnavailableMessage,
                    null);
            }

            await using SyncRootSetupOptionsSession? optionsSession = await _optionsPicker
                .PickAsync(cancellationToken);
            if (optionsSession is null)
            {
                return Cancelled();
            }

            SyncRootSetupOptions options = optionsSession.Options;

            CottonUploadDestinationSnapshot? cloudFolder = await _cloudFolderPicker
                .PickAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            if (cloudFolder is null)
            {
                return Cancelled();
            }

            CottonSyncLocalRootSnapshot? localRoot = await _localRootPicker
                .PickAsync(options.SourceStorageKind, cancellationToken)
                .ConfigureAwait(false);
            if (localRoot is null)
            {
                return Cancelled();
            }

            CottonSyncRootConfigurationResult result = await _configurationService
                .ConfigureRootAsync(
                    instanceUri,
                    accountScopeKey,
                    cloudFolder,
                    localRoot,
                    options.UploadOriginalRetention,
                    cancellationToken)
                .ConfigureAwait(false);
            return result.Status switch
            {
                CottonSyncRootConfigurationStatus.Created => new SyncRootSetupResult(
                    SyncRootSetupStatus.Created,
                    SyncRootSetupResources.CreateCreatedMessage(cloudFolder.Path),
                    result.Root),
                CottonSyncRootConfigurationStatus.Updated => new SyncRootSetupResult(
                    SyncRootSetupStatus.Updated,
                    SyncRootSetupResources.CreateUpdatedMessage(cloudFolder.Path),
                    result.Root),
                CottonSyncRootConfigurationStatus.AlreadyConfigured => new SyncRootSetupResult(
                    SyncRootSetupStatus.AlreadyConfigured,
                    ResolveAlreadyConfiguredMessage(result.Root, options),
                    result.Root),
                _ => throw new InvalidOperationException("Sync root setup status is not supported."),
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
                    SyncRootSetupResources.UnavailableMessage,
                    null);
            }

            CottonSyncLocalRootSnapshot? localRoot = await _localRootPicker
                .PickAsync(root.LocalRoot.StorageKind, cancellationToken)
                .ConfigureAwait(false);
            if (localRoot is null)
            {
                return Cancelled();
            }

            CottonSyncRootSnapshot reconnectedRoot = await _reconnectService
                .ReconnectAsync(root, localRoot, cancellationToken)
                .ConfigureAwait(false);
            return new SyncRootSetupResult(
                SyncRootSetupStatus.Updated,
                SyncRootSetupResources.CreateReconnectedMessage(reconnectedRoot.CloudFolder.Path),
                reconnectedRoot);
        }

        private static SyncRootSetupResult Cancelled()
        {
            return new SyncRootSetupResult(SyncRootSetupStatus.Cancelled, string.Empty, null);
        }

        private static string ResolveAlreadyConfiguredMessage(
            CottonSyncRootSnapshot root,
            SyncRootSetupOptions options)
        {
            if (root.LocalRoot.StorageKind == options.SourceStorageKind)
            {
                return SyncRootSetupResources.AlreadyConfiguredMessage;
            }

            return SyncRootSetupResources.SourceConflictMessage;
        }
    }
}
