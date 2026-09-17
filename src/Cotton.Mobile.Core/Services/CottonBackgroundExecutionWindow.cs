// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonBackgroundExecutionWindow
    {
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(8);
        private readonly TimeSpan _duration;

        public CottonBackgroundExecutionWindow(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || duration > DefaultDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _duration = duration;
        }

        public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();
            using CancellationTokenSource deadline = new(_duration);
            using CancellationTokenSource execution = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            try
            {
                return await operation(execution.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new CottonBackgroundWindowExpiredException(exception);
            }
        }
    }
}
