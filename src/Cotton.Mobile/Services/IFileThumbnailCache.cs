// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IFileThumbnailCache
    {
        string? GetCachedThumbnailSource(string cacheKey);

        Task WarmAsync(
            string cacheKey,
            Uri sourceUri,
            CancellationToken cancellationToken = default);
    }
}
