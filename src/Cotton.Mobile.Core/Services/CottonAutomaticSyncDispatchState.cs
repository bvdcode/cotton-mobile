// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonAutomaticSyncDispatchState : IDisposable
    {
        private readonly Lock _cancellationGate = new();
        private readonly CancellationTokenSource _cancellationSource = new();
        private bool _disposed;

        public bool HasPendingTrigger { get; private set; }

        public CottonAutomaticSyncTrigger PendingTrigger { get; private set; }

        public Task<CottonAutomaticSyncRunResult>? ExecutionTask { get; set; }

        public CancellationToken CancellationToken => _cancellationSource.Token;

        public void Queue(CottonAutomaticSyncTrigger trigger)
        {
            PendingTrigger = HasPendingTrigger
                ? Merge(PendingTrigger, trigger)
                : trigger;
            HasPendingTrigger = true;
        }

        public CottonAutomaticSyncTrigger TakePendingTrigger()
        {
            if (!HasPendingTrigger)
            {
                throw new InvalidOperationException("Automatic sync dispatch state has no pending trigger.");
            }

            CottonAutomaticSyncTrigger trigger = PendingTrigger;
            HasPendingTrigger = false;
            return trigger;
        }

        public void Cancel()
        {
            lock (_cancellationGate)
            {
                if (!_disposed)
                {
                    _cancellationSource.Cancel();
                }
            }
        }

        public void Dispose()
        {
            lock (_cancellationGate)
            {
                if (_disposed)
                {
                    return;
                }

                _cancellationSource.Dispose();
                _disposed = true;
            }
        }

        private static CottonAutomaticSyncTrigger Merge(
            CottonAutomaticSyncTrigger current,
            CottonAutomaticSyncTrigger next)
        {
            return (current, next) switch
            {
                (CottonAutomaticSyncTrigger.ApplicationResumed, CottonAutomaticSyncTrigger.ApplicationResumed) =>
                    CottonAutomaticSyncTrigger.ApplicationResumed,
                (CottonAutomaticSyncTrigger.ApplicationResumed, CottonAutomaticSyncTrigger.PeriodicReconciliation) =>
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                (CottonAutomaticSyncTrigger.ApplicationResumed, CottonAutomaticSyncTrigger.MediaStoreChanged) =>
                    CottonAutomaticSyncTrigger.ApplicationResumed,
                (CottonAutomaticSyncTrigger.PeriodicReconciliation, CottonAutomaticSyncTrigger.ApplicationResumed) =>
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                (CottonAutomaticSyncTrigger.PeriodicReconciliation, CottonAutomaticSyncTrigger.PeriodicReconciliation) =>
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                (CottonAutomaticSyncTrigger.PeriodicReconciliation, CottonAutomaticSyncTrigger.MediaStoreChanged) =>
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                (CottonAutomaticSyncTrigger.MediaStoreChanged, CottonAutomaticSyncTrigger.ApplicationResumed) =>
                    CottonAutomaticSyncTrigger.ApplicationResumed,
                (CottonAutomaticSyncTrigger.MediaStoreChanged, CottonAutomaticSyncTrigger.PeriodicReconciliation) =>
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                (CottonAutomaticSyncTrigger.MediaStoreChanged, CottonAutomaticSyncTrigger.MediaStoreChanged) =>
                    CottonAutomaticSyncTrigger.MediaStoreChanged,
                _ => throw new ArgumentOutOfRangeException(nameof(next), next, "Automatic sync trigger is not supported."),
            };
        }
    }
}
