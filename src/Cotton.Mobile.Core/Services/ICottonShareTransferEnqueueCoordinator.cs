// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonShareTransferEnqueueCoordinator
    {
        Task<CottonShareTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
