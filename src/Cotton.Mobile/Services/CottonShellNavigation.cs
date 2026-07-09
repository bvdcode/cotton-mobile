// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public static class CottonShellNavigation
    {
        private static readonly SemaphoreSlim PushGate = new(1, 1);

        public static Task<bool> PushAsync(Page page, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(page);
            cancellationToken.ThrowIfCancellationRequested();

            return MainThread.IsMainThread
                ? PushOnMainThreadAsync(page, cancellationToken)
                : MainThread.InvokeOnMainThreadAsync(() => PushOnMainThreadAsync(page, cancellationToken));
        }

        private static async Task<bool> PushOnMainThreadAsync(Page page, CancellationToken cancellationToken)
        {
            await PushGate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                INavigation? navigation = Shell.Current?.Navigation;
                if (navigation is null)
                {
                    throw new InvalidOperationException("Shell navigation is unavailable.");
                }

                Page? currentPage = navigation.NavigationStack.LastOrDefault();
                if (currentPage is not null && currentPage.GetType() == page.GetType())
                {
                    return false;
                }

                await navigation.PushAsync(page);
                return true;
            }
            finally
            {
                PushGate.Release();
            }
        }
    }
}
