// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
            await ModalPageNavigation.DismissAsync(_navigation, _page).ConfigureAwait(false);
        }
    }
}
