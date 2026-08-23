// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonSyncRootExecutionLockEntry : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public int ReferenceCount { get; private set; }

        public void AddReference()
        {
            ReferenceCount++;
        }

        public int RemoveReference()
        {
            if (ReferenceCount <= 0)
            {
                throw new InvalidOperationException("Sync-root execution lock has no active references.");
            }

            return --ReferenceCount;
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return _semaphore.WaitAsync(cancellationToken);
        }

        public void Release()
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
