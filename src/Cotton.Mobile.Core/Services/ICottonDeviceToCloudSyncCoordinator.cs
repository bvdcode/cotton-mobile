// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceToCloudSyncCoordinator
    {
        Task<CottonDeviceToCloudSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonDeviceToCloudSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);
    }
}
