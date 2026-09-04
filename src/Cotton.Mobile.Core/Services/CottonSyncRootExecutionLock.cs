// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootExecutionLock
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, CottonSyncRootExecutionLockEntry> _entries =
            new(StringComparer.Ordinal);

        internal int ActiveEntryCount
        {
            get
            {
                lock (_gate)
                {
                    return _entries.Count;
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(
            CottonSyncRootSnapshot root,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(operation);

            string stableKey = root.StableKey;
            CottonSyncRootExecutionLockEntry entry = Rent(stableKey);
            bool lockTaken = false;
            CancellationTokenSource? executionCancellation = null;
            try
            {
                await entry.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
                executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (_gate)
                {
                    entry.ActiveCancellation = executionCancellation;
                }

                return await operation(executionCancellation.Token).ConfigureAwait(false);
            }
            finally
            {
                if (lockTaken)
                {
                    lock (_gate)
                    {
                        entry.ActiveCancellation = null;
                    }

                    executionCancellation?.Dispose();
                    entry.Release();
                }

                Return(stableKey, entry);
            }
        }

        public Task CancelAsync(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);
            lock (_gate)
            {
                return _entries.TryGetValue(root.StableKey, out CottonSyncRootExecutionLockEntry? entry)
                    && entry.ActiveCancellation is not null
                    ? entry.ActiveCancellation.CancelAsync()
                    : Task.CompletedTask;
            }
        }

        private CottonSyncRootExecutionLockEntry Rent(string stableKey)
        {
            lock (_gate)
            {
                if (!_entries.TryGetValue(stableKey, out CottonSyncRootExecutionLockEntry? entry))
                {
                    entry = new CottonSyncRootExecutionLockEntry();
                    _entries.Add(stableKey, entry);
                }

                entry.AddReference();
                return entry;
            }
        }

        private void Return(string stableKey, CottonSyncRootExecutionLockEntry entry)
        {
            lock (_gate)
            {
                if (entry.RemoveReference() != 0)
                {
                    return;
                }

                if (!_entries.Remove(stableKey, out CottonSyncRootExecutionLockEntry? removed)
                    || !ReferenceEquals(entry, removed))
                {
                    throw new InvalidOperationException("Sync-root execution lock entry changed unexpectedly.");
                }

                entry.Dispose();
            }
        }
    }
}
