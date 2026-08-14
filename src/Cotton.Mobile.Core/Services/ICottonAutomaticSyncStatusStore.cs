// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAutomaticSyncStatusStore
    {
        event EventHandler<CottonAutomaticSyncStatusesChangedEventArgs>? StatusesChanged;

        Task<IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Uri instanceUri,
            IReadOnlySet<Guid> activeRootIds,
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> updatedStatuses,
            CancellationToken cancellationToken = default);
    }
}
