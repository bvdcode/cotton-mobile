// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAutomaticSyncRunner
    {
        Task<CottonAutomaticSyncRunResult> RunAsync(
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default);

        Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            CottonAuthenticatedSessionScope sessionScope,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default);
    }
}
