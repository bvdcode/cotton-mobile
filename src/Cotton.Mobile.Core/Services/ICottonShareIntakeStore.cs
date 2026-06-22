// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonShareIntakeStore
    {
        Task<IReadOnlyList<CottonShareIntakeSnapshot>> LoadAsync(CancellationToken cancellationToken = default);

        Task AddAsync(CottonShareIntakeSnapshot snapshot, CancellationToken cancellationToken = default);

        Task SaveAsync(
            IReadOnlyCollection<CottonShareIntakeSnapshot> snapshots,
            CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
