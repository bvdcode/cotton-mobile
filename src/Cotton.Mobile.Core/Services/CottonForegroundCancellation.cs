// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonForegroundCancellation : IAsyncDisposable
    {
        private readonly IApplicationForegroundService _foreground;
        private readonly CancellationTokenSource _cancellation;
        private readonly Lock _gate = new();
        private Task? _stopping;
        private bool _isDisposed;

        public CottonForegroundCancellation(IApplicationForegroundService foreground, CancellationToken cancellationToken)
        {
            _foreground = foreground;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _foreground.Stopped += OnStopped;
            if (!_foreground.IsForeground)
            {
                OnStopped(this, EventArgs.Empty);
            }
        }

        public CancellationToken Token => _cancellation.Token;

        public async ValueTask DisposeAsync()
        {
            _foreground.Stopped -= OnStopped;
            Task stopping;
            lock (_gate)
            {
                _isDisposed = true;
                stopping = _stopping ?? Task.CompletedTask;
            }

            await stopping.ConfigureAwait(false);
            _cancellation.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnStopped(object? sender, EventArgs eventArgs)
        {
            lock (_gate)
            {
                if (!_isDisposed)
                {
                    _stopping ??= _cancellation.CancelAsync();
                }
            }
        }
    }
}
