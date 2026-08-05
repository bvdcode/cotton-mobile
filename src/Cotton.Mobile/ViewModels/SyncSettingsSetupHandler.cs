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
        private readonly ILogger<SyncSettingsSetupHandler> _logger;

        public SyncSettingsSetupHandler(
            SyncSettingsRootProvider rootProvider,
            SyncRootSetupCoordinator rootSetupCoordinator,
            INetworkAccessService networkAccess,
            ILogger<SyncSettingsSetupHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(rootSetupCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _rootSetupCoordinator = rootSetupCoordinator;
            _networkAccess = networkAccess;
            _logger = logger;
        }

        public async Task AddRootAsync(ISyncSettingsViewState state)
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
                    accountScopeKey);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    state.Status = null;
                    return;
                }

                state.ShowRoots(await _rootProvider.LoadAsync(instanceUri, accountScopeKey));
                state.Status = result.Message;
            }
            catch (OperationCanceledException)
            {
                state.Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to add Cotton mobile sync root.");
                state.Status = AppResources.SyncFolderAddFailed;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public async Task ReconnectRootAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item)
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
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    state.ShowRoots(collection);
                    state.Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                if (!root.LocalRoot.RequiresPersistedUserGrant || !root.NeedsUserAction)
                {
                    state.ShowRoots(collection);
                    state.Status = AppResources.LocalFolderAccessAvailable;
                    return;
                }

                SyncRootSetupResult result = await _rootSetupCoordinator.ReconnectLocalRootAsync(root);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    state.Status = null;
                    return;
                }

                if (result.DidChangeRoots)
                {
                    state.ShowRoots(await _rootProvider.LoadAsync(state));
                }

                state.Status = result.Message;
            }
            catch (OperationCanceledException)
            {
                state.Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to reconnect Cotton mobile sync root.");
                state.Status = AppResources.LocalFolderReconnectFailed;
            }
            finally
            {
                state.IsBusy = false;
            }
        }
    }
}
