// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCloudShareLinkService
    {
        Task<CottonCloudShareLinkSnapshot> CreateAsync(
            Uri instanceUri,
            CottonCloudShareLinkRequest request,
            CancellationToken cancellationToken = default);

        Task InvalidateAllFileLinksAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
