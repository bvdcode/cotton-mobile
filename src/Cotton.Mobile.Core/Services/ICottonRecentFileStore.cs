// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonRecentFileStore
    {
        Task<IReadOnlyList<CottonRecentFileSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonRecentFileSnapshot> items,
            CancellationToken cancellationToken = default);

        Task RecordAsync(
            Uri instanceUri,
            CottonRecentFileSnapshot item,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default);

        Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
