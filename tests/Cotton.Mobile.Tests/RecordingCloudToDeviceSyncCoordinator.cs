// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingCloudToDeviceSyncCoordinator : ICottonCloudToDeviceSyncCoordinator
    {
        public int RunRootCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Action? OnRunRoot { get; set; }

        public Task<CottonCloudToDeviceSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonCloudToDeviceSyncRunSummary([]));
        }

        public Task<CottonCloudToDeviceSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();
            LastCancellationToken = cancellationToken;
            RunRootCount++;
            OnRunRoot?.Invoke();
            return Task.FromResult(new CottonCloudToDeviceSyncRunSummary([]));
        }
    }
}
