// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonContentRevisionStore
    {
        Task<CottonContentRevisionIndexSnapshot?> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonContentRevisionIndexSnapshot index,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);
    }
}
