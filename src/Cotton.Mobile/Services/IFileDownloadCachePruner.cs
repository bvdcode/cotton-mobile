// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IFileDownloadCachePruner
    {
        Task PruneAsync(
            Uri instanceUri,
            string? protectedPath = null,
            CancellationToken cancellationToken = default);
    }
}
