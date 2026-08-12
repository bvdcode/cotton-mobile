// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsManagementHandler
    {
        private readonly SyncSettingsRootProvider _rootProvider;
        private readonly SyncRootManager _rootManager;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<SyncSettingsManagementHandler> _logger;

        public SyncSettingsManagementHandler(
            SyncSettingsRootProvider rootProvider,
            SyncRootManager rootManager,
            IUserDialogService dialogService,
            ILogger<SyncSettingsManagementHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(rootManager);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _rootManager = rootManager;
            _dialogService = dialogService;
            _logger = logger;
        }

        public async Task StopRootAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = CottonSyncRootManagementText.StopFailedStatus;
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

                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonSyncRootManagementText.CreateStopTitle(root.CloudFolder.FolderName),
                    CottonSyncRootManagementText.StopMessage,
                    CottonSyncRootManagementText.StopAction,
                    CottonSyncRootManagementText.CancelAction);
                if (!confirmed)
                {
                    state.Status = null;
                    return;
                }

                bool removed = await _rootManager.StopAsync(instanceUri, root);
                state.ShowRoots(await _rootProvider.LoadAsync(state));
                state.Status = removed
                    ? CottonSyncRootManagementText.CreateStoppedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.RootMissingStatus;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to stop Cotton mobile sync root.", exception);
                state.Status = CottonSyncRootManagementText.StopFailedStatus;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public async Task SetRootPausedAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            bool isPaused)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
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

                await _rootManager.SetPausedAsync(instanceUri, root, isPaused);
                state.ShowRoots(await _rootProvider.LoadAsync(state));
                state.Status = isPaused
                    ? CottonSyncRootManagementText.CreatePausedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.CreateResumedStatus(root.CloudFolder.FolderName);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to update Cotton mobile sync root pause state.", exception);
                state.Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
            }
            finally
            {
                state.IsBusy = false;
            }
        }
    }
}
