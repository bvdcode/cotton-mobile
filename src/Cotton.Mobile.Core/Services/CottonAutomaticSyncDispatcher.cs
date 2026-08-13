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

                state.Queue(trigger);
                state.ExecutionTask ??= ExecuteAsync(key, instanceUri, state);
                executionTask = state.ExecutionTask;
            }

            return WaitForCompletionAsync(executionTask, state, cancellationToken);
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
                    CottonAutomaticSyncTrigger trigger;
                    lock (_gate)
                    {
                        if (!state.HasPendingTrigger)
                        {
                            _states.Remove(key);
                            return result;
                        }

                        trigger = state.TakePendingTrigger();
                    }

                    CottonAutomaticSyncRunResult next = await _runner
                        .RunAsync(instanceUri, trigger, state.CancellationToken)
                        .ConfigureAwait(false);
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
