// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingBidirectionalSyncCoordinator : ICottonBidirectionalSyncCoordinator
    {
        public int RunRootCount { get; private set; }

        public Task<CottonBidirectionalSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonBidirectionalSyncRunSummary([]));
        }

        public Task<CottonBidirectionalSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();
            RunRootCount++;
            return Task.FromResult(new CottonBidirectionalSyncRunSummary([]));
        }

        public Task<CottonBidirectionalSyncRunSummary> ExecuteReviewedPlanAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan reviewedPlan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(reviewedPlan);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonBidirectionalSyncRunSummary([]));
        }
    }
}
