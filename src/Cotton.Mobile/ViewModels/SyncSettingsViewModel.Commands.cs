// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class SyncSettingsViewModel
    {
        private AsyncRelayCommand<CottonSyncRootActionRequest> CreatePauseRootCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootActionRequest>(
                (request, cancellationToken) => AsyncCommandExecution.RunAsync(
                    request,
                    (action, token) => _managementHandler.SetRootPausedAsync(this, action.Item, isPaused: true, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                request => request is not null
                    && request.Action == CottonSyncRootAction.Pause
                    && request.Item.CanPauseSync
                    && (!IsBusy || request.Item.IsRunning));
        }

        private void OnRootPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == nameof(CottonSyncRootListItem.IsRunning))
            {
                PauseRootCommand.NotifyCanExecuteChanged();
                RootActionCommand.NotifyCanExecuteChanged();
            }
        }

        private AsyncRelayCommand CreateAddRootCommand()
        {
            return new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    token => _setupHandler.AddRootAsync(this, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                CanAddRoot);
        }

        private AsyncRelayCommand CreateRunAllCommand()
        {
            return new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    token => _executionHandler.RunAllAsync(this, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                CanRunAll);
        }

        private AsyncRelayCommand<CottonSyncRootActionRequest> CreateRootActionCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootActionRequest>(
                (request, cancellationToken) => AsyncCommandExecution.RunAsync(
                    request,
                    ExecuteRootActionAsync,
                    LogUnhandledCommandException,
                    cancellationToken),
                request => !IsBusy && request is not null && CanExecuteRootAction(request));
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private bool CanAddRoot()
        {
            return !IsBusy && _instanceUri is not null && !string.IsNullOrWhiteSpace(_accountScopeKey);
        }

        private void EnterEditMode()
        {
            IsEditMode = true;
        }

        private bool CanEnterEditMode()
        {
            return !IsBusy && IsListVisible && !IsEditMode;
        }

        private void ExitEditMode()
        {
            IsEditMode = false;
        }

        private bool CanExitEditMode()
        {
            return !IsBusy && IsEditMode;
        }

        private static bool CanExecuteRootAction(CottonSyncRootActionRequest request)
        {
            return request.Action switch
            {
                CottonSyncRootAction.ShowFailureDetails => request.Item.CanShowFailureDetails,
                CottonSyncRootAction.ResolvePendingUpload => request.Item.CanResolvePendingUpload,
                CottonSyncRootAction.UsePrimaryAction => request.Item.CanUsePrimaryAction,
                CottonSyncRootAction.Pause => request.Item.CanPauseSync,
                CottonSyncRootAction.Resume => request.Item.CanResumeSync,
                CottonSyncRootAction.Delete => request.Item.CanDeleteSync,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Action,
                    "Sync-root action is not supported."),
            };
        }

        private async Task ExecuteRootActionAsync(
            CottonSyncRootActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            CottonSyncRootListItem item = request.Item;
            switch (request.Action)
            {
                case CottonSyncRootAction.ShowFailureDetails:
                    await _managementHandler.ShowFailureDetailsAsync(item);
                    break;

                case CottonSyncRootAction.ResolvePendingUpload:
                    if (await _managementHandler.ResolvePendingUploadAsync(this, item, cancellationToken))
                    {
                        await _executionHandler.ExecutePrimaryActionAsync(this, item, cancellationToken);
                    }

                    break;

                case CottonSyncRootAction.UsePrimaryAction when item.CanReconnect:
                    await _setupHandler.ReconnectRootAsync(this, item, cancellationToken);
                    break;

                case CottonSyncRootAction.UsePrimaryAction:
                    await _executionHandler.ExecutePrimaryActionAsync(this, item, cancellationToken);
                    break;

                case CottonSyncRootAction.Pause:
                    await _managementHandler.SetRootPausedAsync(this, item, isPaused: true, cancellationToken);
                    break;

                case CottonSyncRootAction.Resume:
                    await _managementHandler.SetRootPausedAsync(this, item, isPaused: false, cancellationToken);
                    CottonSyncRootListItem? resumedRoot = Roots.FirstOrDefault(root => root.Id == item.Id);
                    if (resumedRoot?.CanRunNow == true)
                    {
                        await _executionHandler.ExecutePrimaryActionAsync(this, resumedRoot, cancellationToken);
                    }

                    break;

                case CottonSyncRootAction.Delete:
                    await _managementHandler.DeleteRootAsync(this, item, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException("Sync-root action is not supported.");
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            CottonLog.Error(_logger, "Unhandled Cotton mobile sync settings command failure.", exception);
            Status = AppResources.SyncSettingsUpdateFailed;
        }

    }
}
