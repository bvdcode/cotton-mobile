// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidJobExecution : IAsyncDisposable
    {
        private readonly Lock _gate = new();
        private readonly CancellationTokenSource _cancellation = new();
        private Task? _stopping;
        private bool _isDisposed;

        public CancellationToken Token => _cancellation.Token;

        public bool IsCancellationRequested => _cancellation.IsCancellationRequested;

        public Task CancelAsync()
        {
            lock (_gate)
            {
                return _isDisposed ? Task.CompletedTask : _stopping ??= _cancellation.CancelAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Task stopping;
            lock (_gate)
            {
                _isDisposed = true;
                stopping = _stopping ?? Task.CompletedTask;
            }

            try
            {
                await stopping.ConfigureAwait(false);
            }
            finally
            {
                _cancellation.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
