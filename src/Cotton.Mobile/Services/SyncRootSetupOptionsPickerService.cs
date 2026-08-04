// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupOptionsPickerService : ISyncRootSetupOptionsPickerService
    {
        public async Task<SyncRootSetupOptionsSession?> PickAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<SyncRootSetupOptions?> completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            SyncRootSetupOptionsViewModel viewModel = new(
                options => completion.TrySetResult(options));
            SyncRootSetupOptionsPage page = new(viewModel);
            INavigation navigation = await MainThread.InvokeOnMainThreadAsync(GetNavigation);

            await MainThread.InvokeOnMainThreadAsync(
                () => navigation.PushModalAsync(page, animated: false));
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            bool keepPageOpen = false;
            try
            {
                SyncRootSetupOptions? options = await completion.Task.ConfigureAwait(false);
                if (options is null)
                {
                    return null;
                }

                SyncRootSetupOptionsSession session = new(options, navigation, page);
                keepPageOpen = true;
                return session;
            }
            finally
            {
                if (!keepPageOpen)
                {
                    await SyncRootSetupOptionsSession
                        .DismissAsync(navigation, page)
                        .ConfigureAwait(false);
                }
            }
        }

        private static INavigation GetNavigation()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
                ?? throw new InvalidOperationException("Sync setup options need an active page.");
        }
    }
}
