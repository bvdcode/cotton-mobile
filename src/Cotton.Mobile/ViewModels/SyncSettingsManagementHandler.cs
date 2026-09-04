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
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly CottonSyncRootExecutionLock _executionLock;
        private readonly ILogger<SyncSettingsManagementHandler> _logger;

        public SyncSettingsManagementHandler(
            SyncSettingsRootProvider rootProvider,
            SyncRootManager rootManager,
            IUserDialogService dialogService,
            ICottonUploadReceiptStore uploadReceiptStore,
            CottonSyncRootExecutionLock executionLock,
            ILogger<SyncSettingsManagementHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(rootManager);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);
            ArgumentNullException.ThrowIfNull(executionLock);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _rootManager = rootManager;
            _dialogService = dialogService;
            _uploadReceiptStore = uploadReceiptStore;
            _executionLock = executionLock;
            _logger = logger;
        }

        public async Task DeleteRootAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = CottonSyncRootManagementText.DeleteFailedStatus;
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

                cancellationToken.ThrowIfCancellationRequested();
                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonSyncRootManagementText.CreateDeleteTitle(root.CloudFolder.FolderName),
                    CottonSyncRootManagementText.DeleteMessage,
                    CottonSyncRootManagementText.DeleteAction,
                    CottonSyncRootManagementText.CancelAction);
                cancellationToken.ThrowIfCancellationRequested();
                if (!confirmed)
                {
                    state.Status = null;
                    return;
                }

                bool removed = await _rootManager.DeleteAsync(instanceUri, root, cancellationToken);
                state.ShowRoots(await _rootProvider.LoadAsync(state, cancellationToken));
                state.Status = removed
                    ? CottonSyncRootManagementText.CreateDeletedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.RootMissingStatus;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to delete Cotton mobile sync root.", exception);
                state.Status = CottonSyncRootManagementText.DeleteFailedStatus;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public Task ShowFailureDetailsAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!item.CanShowFailureDetails)
            {
                throw new ArgumentException("Sync root does not have failure details.", nameof(item));
            }

            return _dialogService.ShowAlertAsync(
                CottonSyncRootManagementText.CreateFailureDetailsTitle(item.Title),
                item.FailureDetails,
                CottonSyncRootManagementText.CloseAction);
        }

        public async Task<bool> ResolvePendingUploadAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null || !item.CanResolvePendingUpload)
            {
                state.Status = CottonSyncRootManagementText.PendingUploadResolveFailedStatus;
                return false;
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
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonSyncRootManagementText.CreateResolvePendingUploadTitle(root.CloudFolder.FolderName),
                    CottonSyncRootManagementText.ResolvePendingUploadMessage,
                    CottonSyncRootManagementText.ResolvePendingUploadAction,
                    CottonSyncRootManagementText.CancelAction);
                cancellationToken.ThrowIfCancellationRequested();
                if (!confirmed)
                {
                    state.Status = null;
                    return false;
                }

                int clearedCount = await _executionLock.ExecuteAsync(
                    root,
                    token => _uploadReceiptStore.ClearPendingAsync(instanceUri, root, token),
                    cancellationToken);
                if (clearedCount == 0)
                {
                    state.Status = CottonSyncRootManagementText.PendingUploadResolveFailedStatus;
                    return false;
                }

                state.Status = CottonSyncRootManagementText.PendingUploadResolvedStatus;
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to resolve pending Cotton mobile uploads.", exception);
                state.Status = CottonSyncRootManagementText.PendingUploadResolveFailedStatus;
                return false;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public async Task SetRootPausedAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            bool isPaused,
            CancellationToken cancellationToken = default)
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

                await _rootManager.SetPausedAsync(instanceUri, root, isPaused, cancellationToken);
                if (isPaused)
                {
                    await _executionLock.CancelAsync(root);
                }

                state.ShowRoots(await _rootProvider.LoadAsync(state, cancellationToken));
                state.Status = isPaused
                    ? CottonSyncRootManagementText.CreatePausedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.CreateResumedStatus(root.CloudFolder.FolderName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to update Cotton mobile sync root pause state.", exception);
                state.Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
            }
        }
    }
}
