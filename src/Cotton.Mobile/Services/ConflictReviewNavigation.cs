// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class ConflictReviewNavigation(
        CottonRemoteConflictResolutionService service,
        ICottonAutomaticSyncBackgroundScheduler scheduler,
        IUserDialogService dialogs,
        ILoggerFactory loggerFactory,
        CottonSyncProgressHub progressHub,
        ICottonAutomaticSyncStatusStore statusStore)
    {
        public async Task ShowAsync(CottonSyncRootSnapshot root, CancellationToken cancellationToken)
        {
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using ConflictReviewViewModel viewModel = new(root, service, scheduler, dialogs,
                loggerFactory.CreateLogger<ConflictReviewViewModel>(), () => completion.TrySetResult(),
                progressHub, statusStore);
            ConflictReviewPage page = new(viewModel);
            INavigation navigation = await ModalPageNavigation.ShowAsync(page);
            using CancellationTokenRegistration registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            try
            {
                await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                viewModel.Close();
                await ModalPageNavigation.DismissAsync(navigation, page);
                await (viewModel.RefreshCommand.ExecutionTask ?? Task.CompletedTask).ConfigureAwait(false);
                await (viewModel.ReplaceCommand.ExecutionTask ?? Task.CompletedTask).ConfigureAwait(false);
            }
        }
    }
}
