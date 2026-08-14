// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsSetupHandler
    {
        private readonly SyncSettingsRootProvider _rootProvider;
        private readonly SyncRootSetupCoordinator _rootSetupCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly CottonAutomaticSyncDispatcher _automaticSyncDispatcher;
        private readonly ICottonAutomaticSyncBackgroundScheduler _backgroundScheduler;
        private readonly ILogger<SyncSettingsSetupHandler> _logger;

        public SyncSettingsSetupHandler(
            SyncSettingsRootProvider rootProvider,
            SyncRootSetupCoordinator rootSetupCoordinator,
            INetworkAccessService networkAccess,
            CottonAutomaticSyncDispatcher automaticSyncDispatcher,
            ICottonAutomaticSyncBackgroundScheduler backgroundScheduler,
            ILogger<SyncSettingsSetupHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(rootSetupCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(automaticSyncDispatcher);
            ArgumentNullException.ThrowIfNull(backgroundScheduler);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _rootSetupCoordinator = rootSetupCoordinator;
            _networkAccess = networkAccess;
            _automaticSyncDispatcher = automaticSyncDispatcher;
            _backgroundScheduler = backgroundScheduler;
            _logger = logger;
        }

        public async Task AddRootAsync(
            ISyncSettingsViewState state,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            Uri? instanceUri = state.InstanceUri;
            string? accountScopeKey = state.AccountScopeKey;
            if (instanceUri is null || string.IsNullOrWhiteSpace(accountScopeKey))
            {
                state.Status = AppResources.SyncFolderAccountUnavailable;
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                state.Status = AppResources.SyncFolderAddOffline;
                return;
            }

            state.IsBusy = true;
            try
            {
                SyncRootSetupResult result = await _rootSetupCoordinator.AddRootAsync(
                    instanceUri,
                    accountScopeKey,
                    cancellationToken);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    state.Status = null;
                    return;
                }

                state.ShowRoots(await _rootProvider.LoadAsync(instanceUri, accountScopeKey, cancellationToken));
                state.Status = result.Message;
                QueueChangedRoot(state, instanceUri, accountScopeKey, result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to add Cotton mobile sync root.", exception);
                state.Status = AppResources.SyncFolderAddFailed;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public async Task ReconnectRootAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = AppResources.LocalFolderReconnectFailed;
                return;
            }

            state.IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state, cancellationToken);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    state.ShowRoots(collection);
                    state.Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                if (!root.NeedsUserAction)
                {
                    state.ShowRoots(collection);
                    state.Status = AppResources.LocalFolderAccessAvailable;
                    return;
                }

                SyncRootSetupResult result = await _rootSetupCoordinator
                    .ReconnectLocalRootAsync(root, cancellationToken);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    state.Status = null;
                    return;
                }

                if (result.DidChangeRoots)
                {
                    state.ShowRoots(await _rootProvider.LoadAsync(state, cancellationToken));
                }

                state.Status = result.Message;
                QueueChangedRoot(state, instanceUri, state.AccountScopeKey, result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to reconnect Cotton mobile sync root.", exception);
                state.Status = AppResources.LocalFolderReconnectFailed;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        private void QueueChangedRoot(
            ISyncSettingsViewState state,
            Uri instanceUri,
            string? accountScopeKey,
            SyncRootSetupResult setupResult)
        {
            if (!setupResult.DidChangeRoots)
            {
                return;
            }

            CottonSyncRootSnapshot root = setupResult.Root
                ?? throw new InvalidOperationException("Changed sync setup did not return its root.");
            _ = RunChangedRootAsync(state, instanceUri, accountScopeKey, root.Id);
        }

        private async Task RunChangedRootAsync(
            ISyncSettingsViewState state,
            Uri instanceUri,
            string? accountScopeKey,
            Guid rootId)
        {
            try
            {
                CottonAutomaticSyncRunResult result = await _automaticSyncDispatcher
                    .RunRootsAsync(instanceUri, [rootId], CancellationToken.None);
                if (result.HasFailures)
                {
                    await _backgroundScheduler.ScheduleRootRetriesAsync(
                        result.FailedRootIds,
                        CancellationToken.None);
                }

                if (IsCurrentAccount(state, instanceUri, accountScopeKey))
                {
                    state.Status = null;
                }
            }
            catch (OperationCanceledException exception)
            {
                CottonLog.Debug(_logger, "Automatic sync for a changed root was canceled.", exception);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonLog.Warning(_logger, "Failed to start automatic sync for a changed root.", exception);
                if (IsCurrentAccount(state, instanceUri, accountScopeKey))
                {
                    state.Status = AppResources.SyncInitialRunFailed;
                }
            }
        }

        private static bool IsCurrentAccount(
            ISyncSettingsViewState state,
            Uri instanceUri,
            string? accountScopeKey)
        {
            return Uri.Equals(state.InstanceUri, instanceUri)
                && string.Equals(state.AccountScopeKey, accountScopeKey, StringComparison.Ordinal);
        }
    }
}
