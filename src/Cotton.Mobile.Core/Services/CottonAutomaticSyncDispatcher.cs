// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncDispatcher(ICottonAutomaticSyncRunner runner)
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<(string InstanceUri, string AccountScopeKey), CottonAutomaticSyncDispatchState>
            _states = [];
        private readonly ICottonAutomaticSyncRunner _runner =
            runner ?? throw new ArgumentNullException(nameof(runner));

        public Task<CottonAutomaticSyncRunResult> RunAsync(
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sessionScope);
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            return QueueAsync(
                sessionScope,
                state => state.Queue(trigger),
                cancellationToken);
        }

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            CottonAuthenticatedSessionScope sessionScope,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sessionScope);
            ArgumentNullException.ThrowIfNull(rootIds);
            Guid[] selectedRootIds = [.. rootIds.Distinct().Order()];
            if (selectedRootIds.Length == 0 || selectedRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids are required.", nameof(rootIds));
            }

            return QueueAsync(
                sessionScope,
                state => state.QueueRoots(selectedRootIds),
                cancellationToken);
        }

        public void Cancel(CottonAuthenticatedSessionScope sessionScope)
        {
            ArgumentNullException.ThrowIfNull(sessionScope);
            CottonAutomaticSyncDispatchState? state;
            lock (_gate)
            {
                _states.TryGetValue(CreateKey(sessionScope), out state);
            }

            state?.Cancel();
        }

        private Task<CottonAutomaticSyncRunResult> QueueAsync(
            CottonAuthenticatedSessionScope sessionScope,
            Action<CottonAutomaticSyncDispatchState> queue,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string InstanceUri, string AccountScopeKey) key = CreateKey(sessionScope);
            CottonAutomaticSyncDispatchState state;
            Task<CottonAutomaticSyncRunResult> executionTask;
            lock (_gate)
            {
                if (!_states.TryGetValue(key, out state!))
                {
                    state = new CottonAutomaticSyncDispatchState();
                    _states.Add(key, state);
                }

                queue(state);
                state.ExecutionTask ??= ExecuteAsync(key, sessionScope, state);
                executionTask = state.ExecutionTask;
            }

            return executionTask.WaitAsync(cancellationToken);
        }

        private async Task<CottonAutomaticSyncRunResult> ExecuteAsync(
            (string InstanceUri, string AccountScopeKey) key,
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncDispatchState state)
        {
            CottonAutomaticSyncRunResult result = CottonAutomaticSyncRunResult.Empty;
            try
            {
                while (true)
                {
                    CottonAutomaticSyncDispatchRequest request;
                    lock (_gate)
                    {
                        if (!state.HasPendingRequest)
                        {
                            _states.Remove(key);
                            return result;
                        }

                        request = state.TakePendingRequest();
                    }

                    CottonAutomaticSyncRunResult next;
                    if (request.Trigger.HasValue)
                    {
                        next = await _runner
                            .RunAsync(sessionScope, request.Trigger.Value, state.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        next = await _runner
                            .RunRootsAsync(sessionScope, request.RootIds, state.CancellationToken)
                            .ConfigureAwait(false);
                    }

                    result = result.Merge(next);
                }
            }
            finally
            {
                lock (_gate)
                {
                    if (_states.TryGetValue(key, out CottonAutomaticSyncDispatchState? current)
                        && ReferenceEquals(current, state))
                    {
                        _states.Remove(key);
                    }
                }

                state.Dispose();
            }
        }

        private static (string InstanceUri, string AccountScopeKey) CreateKey(
            CottonAuthenticatedSessionScope sessionScope)
        {
            return (sessionScope.InstanceUri.AbsoluteUri, sessionScope.AccountScopeKey);
        }
    }
}
