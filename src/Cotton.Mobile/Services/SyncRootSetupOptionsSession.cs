// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupOptionsSession : IAsyncDisposable
    {
        private readonly INavigation _navigation;
        private readonly Page _page;
        private bool _isDisposed;

        internal SyncRootSetupOptionsSession(
            SyncRootSetupOptions options,
            INavigation navigation,
            Page page)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(navigation);
            ArgumentNullException.ThrowIfNull(page);

            Options = options;
            _navigation = navigation;
            _page = page;
        }

        public SyncRootSetupOptions Options { get; }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
            await DismissAsync(_navigation, _page).ConfigureAwait(false);
        }

        internal static Task DismissAsync(INavigation navigation, Page page)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IReadOnlyList<Page> modalStack = navigation.ModalStack;
                if (!modalStack.Contains(page))
                {
                    return;
                }

                if (!ReferenceEquals(modalStack[^1], page))
                {
                    throw new InvalidOperationException(
                        "Sync setup options must be the active modal page before dismissal.");
                }

                await navigation.PopModalAsync(animated: false);
            });
        }
    }
}
