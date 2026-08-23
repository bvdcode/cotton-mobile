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
        private readonly ICottonSyncRootSetupDraftStore _draftStore;
        private readonly ICottonSyncRootStore _rootStore;

        public SyncRootSetupCoordinator(
            ISyncRootSetupOptionsPickerService optionsPicker,
            ICloudFolderPickerService cloudFolderPicker,
            ICottonSyncLocalRootPickerService localRootPicker,
            CottonSyncRootConfigurationService configurationService,
            CottonSyncRootReconnectService reconnectService,
            ICottonSyncRootSetupDraftStore draftStore,
            ICottonSyncRootStore rootStore)
        {
            ArgumentNullException.ThrowIfNull(optionsPicker);
            ArgumentNullException.ThrowIfNull(cloudFolderPicker);
            ArgumentNullException.ThrowIfNull(localRootPicker);
            ArgumentNullException.ThrowIfNull(configurationService);
            ArgumentNullException.ThrowIfNull(reconnectService);
            ArgumentNullException.ThrowIfNull(draftStore);
            ArgumentNullException.ThrowIfNull(rootStore);

            _optionsPicker = optionsPicker;
            _cloudFolderPicker = cloudFolderPicker;
            _localRootPicker = localRootPicker;
            _configurationService = configurationService;
            _reconnectService = reconnectService;
            _draftStore = draftStore;
            _rootStore = rootStore;
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

            Guid requestId = Guid.NewGuid();
            CottonSyncRootSetupDraft draft = new(
                requestId,
                CottonSyncRootSetupOperation.Add,
                instanceUri,
                accountScopeKey,
                options.SourceStorageKind,
                options.UploadOriginalRetention,
                cloudFolder,
                rootId: null);
            await _draftStore.SaveAsync(draft, cancellationToken).ConfigureAwait(false);
            CottonSyncLocalRootSnapshot? localRoot = await PickLocalRootAsync(
                options.SourceStorageKind,
                requestId,
                cancellationToken).ConfigureAwait(false);
            if (localRoot is null)
            {
                await CompleteDraftAsync(requestId, cancellationToken).ConfigureAwait(false);
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
            await CompleteDraftAsync(requestId, cancellationToken).ConfigureAwait(false);
            return CreateConfigurationResult(result, cloudFolder, options);
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

            Guid requestId = Guid.NewGuid();
            CottonSyncRootSetupDraft draft = new(
                requestId,
                CottonSyncRootSetupOperation.Reconnect,
                root.InstanceUri,
                root.AccountScopeKey,
                root.LocalRoot.StorageKind,
                root.UploadOriginalRetention,
                destination: null,
                root.Id);
            await _draftStore.SaveAsync(draft, cancellationToken).ConfigureAwait(false);
            CottonSyncLocalRootSnapshot? localRoot = await PickLocalRootAsync(
                root.LocalRoot.StorageKind,
                requestId,
                cancellationToken).ConfigureAwait(false);
            if (localRoot is null)
            {
                await CompleteDraftAsync(requestId, cancellationToken).ConfigureAwait(false);
                return Cancelled();
            }

            CottonSyncRootSnapshot reconnectedRoot = await _reconnectService
                .ReconnectAsync(root, localRoot, cancellationToken)
                .ConfigureAwait(false);
            await CompleteDraftAsync(requestId, cancellationToken).ConfigureAwait(false);
            return new SyncRootSetupResult(
                SyncRootSetupStatus.Updated,
                SyncRootSetupResources.CreateReconnectedMessage(reconnectedRoot.CloudFolder.Path),
                reconnectedRoot);
        }

        public async Task<SyncRootSetupResult?> ResumePendingAsync(
            Uri instanceUri,
            string accountScopeKey,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            CottonSyncRootSetupDraft? draft = await _draftStore
                .LoadAsync(cancellationToken)
                .ConfigureAwait(false);
            if (draft is null
                || !Uri.Equals(draft.InstanceUri, instanceUri)
                || !string.Equals(draft.AccountScopeKey, accountScopeKey, StringComparison.Ordinal))
            {
                return null;
            }

            CottonSyncLocalRootSnapshot? localRoot = await PickLocalRootAsync(
                draft.StorageKind,
                draft.RequestId,
                cancellationToken).ConfigureAwait(false);
            if (localRoot is null)
            {
                await CompleteDraftAsync(draft.RequestId, cancellationToken).ConfigureAwait(false);
                return Cancelled();
            }

            SyncRootSetupResult result = draft.Operation switch
            {
                CottonSyncRootSetupOperation.Add => await ResumeAddAsync(
                    draft,
                    localRoot,
                    cancellationToken).ConfigureAwait(false),
                CottonSyncRootSetupOperation.Reconnect => await ResumeReconnectAsync(
                    draft,
                    localRoot,
                    cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidDataException("Saved sync-root setup operation is not supported."),
            };
            await CompleteDraftAsync(draft.RequestId, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private async Task<SyncRootSetupResult> ResumeAddAsync(
            CottonSyncRootSetupDraft draft,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken)
        {
            CottonUploadDestinationSnapshot destination = draft.Destination
                ?? throw new InvalidDataException("Saved sync-root destination is unavailable.");
            CottonSyncRootConfigurationResult configuration = await _configurationService
                .ConfigureRootAsync(
                    draft.InstanceUri,
                    draft.AccountScopeKey,
                    destination,
                    localRoot,
                    draft.Retention,
                    cancellationToken)
                .ConfigureAwait(false);
            SyncRootSetupOptions options = new(draft.StorageKind, draft.Retention);
            return CreateConfigurationResult(configuration, destination, options);
        }

        private async Task<SyncRootSetupResult> ResumeReconnectAsync(
            CottonSyncRootSetupDraft draft,
            CottonSyncLocalRootSnapshot localRoot,
            CancellationToken cancellationToken)
        {
            Guid rootId = draft.RootId
                ?? throw new InvalidDataException("Saved reconnect root is unavailable.");
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore
                .LoadAsync(draft.InstanceUri, cancellationToken)
                .ConfigureAwait(false);
            CottonSyncRootSnapshot root = roots.SingleOrDefault(candidate =>
                    candidate.Id == rootId
                    && string.Equals(candidate.AccountScopeKey, draft.AccountScopeKey, StringComparison.Ordinal))
                ?? throw new InvalidDataException("Saved reconnect root no longer exists.");
            CottonSyncRootSnapshot reconnectedRoot = await _reconnectService
                .ReconnectAsync(root, localRoot, cancellationToken)
                .ConfigureAwait(false);
            return new SyncRootSetupResult(
                SyncRootSetupStatus.Updated,
                SyncRootSetupResources.CreateReconnectedMessage(reconnectedRoot.CloudFolder.Path),
                reconnectedRoot);
        }

        private async Task CompleteDraftAsync(Guid requestId, CancellationToken cancellationToken)
        {
            _localRootPicker.CompletePick(requestId);
            await _draftStore.ClearAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<CottonSyncLocalRootSnapshot?> PickLocalRootAsync(
            CottonSyncRootStorageKind storageKind,
            Guid requestId,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _localRootPicker
                    .PickAsync(storageKind, requestId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CompleteDraftAsync(requestId, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        private static SyncRootSetupResult CreateConfigurationResult(
            CottonSyncRootConfigurationResult result,
            CottonUploadDestinationSnapshot cloudFolder,
            SyncRootSetupOptions options)
        {
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
