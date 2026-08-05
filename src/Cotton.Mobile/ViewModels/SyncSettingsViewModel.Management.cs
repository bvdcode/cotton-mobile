// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class SyncSettingsViewModel
    {
        private async Task StopRootAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = CottonSyncRootManagementText.StopFailedStatus;
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

                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonSyncRootManagementText.CreateStopTitle(root.CloudFolder.FolderName),
                    CottonSyncRootManagementText.StopMessage,
                    CottonSyncRootManagementText.StopAction,
                    CottonSyncRootManagementText.CancelAction);
                if (!confirmed)
                {
                    Status = null;
                    return;
                }

                bool removed = await _rootManager.StopAsync(instanceUri, root);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
                Status = removed
                    ? CottonSyncRootManagementText.CreateStoppedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.RootMissingStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to stop Cotton mobile sync root.");
                Status = CottonSyncRootManagementText.StopFailedStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetRootPausedAsync(CottonSyncRootListItem item, bool isPaused)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
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

                await _rootManager.SetPausedAsync(instanceUri, root, isPaused);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
                Status = isPaused
                    ? CottonSyncRootManagementText.CreatePausedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.CreateResumedStatus(root.CloudFolder.FolderName);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to update Cotton mobile sync root pause state.");
                Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
