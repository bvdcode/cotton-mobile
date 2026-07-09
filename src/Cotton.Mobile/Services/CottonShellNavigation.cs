// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public static class CottonShellNavigation
    {
        private static readonly SemaphoreSlim PushGate = new(1, 1);

        public static Task<bool> PushAsync(
            Page page,
            CancellationToken cancellationToken = default,
            Func<Page, bool>? isDuplicateTopPage = null)
        {
            ArgumentNullException.ThrowIfNull(page);
            cancellationToken.ThrowIfCancellationRequested();

            return MainThread.IsMainThread
                ? PushOnMainThreadAsync(page, cancellationToken, isDuplicateTopPage)
                : MainThread.InvokeOnMainThreadAsync(() =>
                    PushOnMainThreadAsync(page, cancellationToken, isDuplicateTopPage));
        }

        private static async Task<bool> PushOnMainThreadAsync(
            Page page,
            CancellationToken cancellationToken,
            Func<Page, bool>? isDuplicateTopPage)
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
                if (currentPage is not null && isDuplicateTopPage?.Invoke(currentPage) == true)
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
