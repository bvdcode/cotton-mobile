// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile.Services
{
    public interface ICottonProfileCacheStore
    {
        Task<MainPageProfile?> GetAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task SaveAsync(Uri instanceUri, MainPageProfile profile, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
