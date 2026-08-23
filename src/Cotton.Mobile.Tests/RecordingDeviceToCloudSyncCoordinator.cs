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

        public Guid? BlockedRootId { get; set; }

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

            if (root.Id == BlockedRootId)
            {
                CottonDeviceToCloudSyncPlanItem blockedItem = new(
                    CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged,
                    CottonFileBrowserEntryType.File,
                    "blocked.txt",
                    "blocked.txt",
                    cloudItemId: null,
                    expectedRemoteETag: null,
                    DateTime.UtcNow,
                    1,
                    "text/plain",
                    "blocked-source",
                    Guid.NewGuid(),
                    TestContentHashes.First);
                CottonDeviceToCloudSyncPlanSnapshot plan = new(
                    root.Id,
                    root.CloudFolder.FolderId,
                    root.CloudFolder.FolderName,
                    [blockedItem]);
                CottonDeviceToCloudSyncExecutionResult executionResult = new(0, 0, 0, 0, 0, 1);
                CottonDeviceToCloudSyncRootRunResult rootResult =
                    CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult);
                return Task.FromResult(new CottonDeviceToCloudSyncRunSummary([rootResult]));
            }

            return Task.FromResult(new CottonDeviceToCloudSyncRunSummary([]));
        }
    }
}
