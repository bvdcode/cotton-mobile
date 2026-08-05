// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class SyncSettingsViewModel
    {
        private async Task AddRootAsync()
        {
            Uri? instanceUri = _instanceUri;
            string? accountScopeKey = _accountScopeKey;
            if (instanceUri is null || string.IsNullOrWhiteSpace(accountScopeKey))
            {
                Status = "Could not add a sync folder for this account.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = "Connect to the internet to add a sync folder.";
                return;
            }

            IsBusy = true;
            try
            {
                SyncRootSetupResult result = await _rootSetupCoordinator.AddRootAsync(
                    instanceUri,
                    accountScopeKey);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    Status = null;
                    return;
                }

                await LoadRootsAsync(instanceUri);
                Status = result.Message;
            }
            catch (OperationCanceledException)
            {
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to add Cotton mobile sync root.");
                Status = "Could not add this sync folder.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReconnectRootAsync(CottonSyncRootListItem item)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not reconnect this local folder.";
                return;
            }

            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    ShowRoots(collection);
                    Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                if (!root.LocalRoot.RequiresPersistedUserGrant || !root.NeedsUserAction)
                {
                    ShowRoots(collection);
                    Status = "Local folder access is already available.";
                    return;
                }

                SyncRootSetupResult result = await _rootSetupCoordinator.ReconnectLocalRootAsync(root);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    Status = null;
                    return;
                }

                if (result.DidChangeRoots)
                {
                    ShowRoots(await LoadRootCollectionAsync(instanceUri));
                }

                Status = result.Message;
            }
            catch (OperationCanceledException)
            {
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to reconnect Cotton mobile sync root.");
                Status = "Could not reconnect this local folder.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
