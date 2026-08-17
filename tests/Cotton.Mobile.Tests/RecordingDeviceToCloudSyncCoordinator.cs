// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingDeviceToCloudSyncCoordinator : ICottonDeviceToCloudSyncCoordinator
    {
        public int RunRootCount { get; private set; }

        public List<Guid> RootIds { get; } = [];

        public Guid? FailingRootId { get; set; }

        public Exception FailureException { get; set; } = new IOException("Simulated sync root failure.");

        public Task<CottonDeviceToCloudSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonDeviceToCloudSyncRunSummary([]));
        }

        public Task<CottonDeviceToCloudSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();
            RunRootCount++;
            RootIds.Add(root.Id);
            if (root.Id == FailingRootId)
            {
                throw FailureException;
            }

            return Task.FromResult(new CottonDeviceToCloudSyncRunSummary([]));
        }
    }
}
