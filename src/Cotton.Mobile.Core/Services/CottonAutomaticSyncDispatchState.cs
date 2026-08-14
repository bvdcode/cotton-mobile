// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonAutomaticSyncDispatchState : IDisposable
    {
        private readonly Lock _cancellationGate = new();
        private readonly CancellationTokenSource _cancellationSource = new();
        private readonly HashSet<Guid> _pendingRootIds = [];
        private bool _disposed;

        public bool HasPendingRequest => PendingTrigger.HasValue || _pendingRootIds.Count > 0;

        public CottonAutomaticSyncTrigger? PendingTrigger { get; private set; }

        public Task<CottonAutomaticSyncRunResult>? ExecutionTask { get; set; }

        public CancellationToken CancellationToken => _cancellationSource.Token;

        public void Queue(CottonAutomaticSyncTrigger trigger)
        {
            PendingTrigger = PendingTrigger.HasValue
                ? Merge(PendingTrigger.Value, trigger)
                : trigger;
            _pendingRootIds.Clear();
        }

        public void QueueRoots(IReadOnlyCollection<Guid> rootIds)
        {
            ArgumentNullException.ThrowIfNull(rootIds);
            if (rootIds.Count == 0 || rootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids are required.", nameof(rootIds));
            }

            if (PendingTrigger.HasValue)
            {
                return;
            }

            _pendingRootIds.UnionWith(rootIds);
        }

        public CottonAutomaticSyncDispatchRequest TakePendingRequest()
        {
            if (PendingTrigger.HasValue)
            {
                CottonAutomaticSyncTrigger trigger = PendingTrigger.Value;
                PendingTrigger = null;
                return CottonAutomaticSyncDispatchRequest.ForTrigger(trigger);
            }

            if (_pendingRootIds.Count == 0)
            {
                throw new InvalidOperationException("Automatic sync dispatch state has no pending request.");
            }

            CottonAutomaticSyncDispatchRequest request =
                CottonAutomaticSyncDispatchRequest.ForRoots(_pendingRootIds);
            _pendingRootIds.Clear();
            return request;
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
