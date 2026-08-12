// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonBidirectionalSyncCoordinator
    {
        Task<CottonBidirectionalSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonBidirectionalSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);

        Task<CottonBidirectionalSyncRunSummary> ExecuteReviewedPlanAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan reviewedPlan,
            CancellationToken cancellationToken = default);
    }
}
