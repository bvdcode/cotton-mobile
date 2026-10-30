// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.Concurrent;

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootExecutionLock
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks =
            new(StringComparer.Ordinal);

        public async Task<T> ExecuteAsync<T>(
            CottonSyncRootSnapshot root,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(operation);

            SemaphoreSlim executionLock = _locks.GetOrAdd(
                root.StableKey,
                static _ => new SemaphoreSlim(1, 1));
            await executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                executionLock.Release();
            }
        }
    }
}
