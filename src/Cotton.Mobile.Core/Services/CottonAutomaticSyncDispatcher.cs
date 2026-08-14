// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncDispatcher(ICottonAutomaticSyncRunner runner)
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, CottonAutomaticSyncDispatchState> _states =
            new(StringComparer.Ordinal);
        private readonly ICottonAutomaticSyncRunner _runner =
            runner ?? throw new ArgumentNullException(nameof(runner));

        public Task<CottonAutomaticSyncRunResult> RunAsync(
            Uri instanceUri,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            return QueueAsync(
                instanceUri,
                state => state.Queue(trigger),
                cancellationToken);
        }

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            Uri instanceUri,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(rootIds);
            Guid[] selectedRootIds = [.. rootIds.Distinct().Order()];
            if (selectedRootIds.Length == 0 || selectedRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids are required.", nameof(rootIds));
            }

            return QueueAsync(
                instanceUri,
                state => state.QueueRoots(selectedRootIds),
                cancellationToken);
        }

        public void Cancel(Uri instanceUri)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            CottonAutomaticSyncDispatchState? state;
            lock (_gate)
            {
                _states.TryGetValue(instanceUri.AbsoluteUri, out state);
            }

            state?.Cancel();
        }

        private Task<CottonAutomaticSyncRunResult> QueueAsync(
            Uri instanceUri,
            Action<CottonAutomaticSyncDispatchState> queue,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string key = instanceUri.AbsoluteUri;
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
                state.ExecutionTask ??= ExecuteAsync(key, instanceUri, state);
                executionTask = state.ExecutionTask;
            }

            return WaitForCompletionAsync(executionTask, state, cancellationToken);
        }

        private async Task<CottonAutomaticSyncRunResult> ExecuteAsync(
            string key,
            Uri instanceUri,
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
                            .RunAsync(instanceUri, request.Trigger.Value, state.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        next = await _runner
                            .RunRootsAsync(instanceUri, request.RootIds, state.CancellationToken)
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

        private static async Task<CottonAutomaticSyncRunResult> WaitForCompletionAsync(
            Task<CottonAutomaticSyncRunResult> executionTask,
            CottonAutomaticSyncDispatchState state,
            CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(
                static dispatchState => ((CottonAutomaticSyncDispatchState)dispatchState!).Cancel(),
                state);
            return await executionTask.ConfigureAwait(false);
        }
    }
}
