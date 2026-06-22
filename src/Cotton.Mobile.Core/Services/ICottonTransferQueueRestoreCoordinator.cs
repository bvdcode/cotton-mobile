// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonTransferQueueRestoreCoordinator
    {
        Task<IReadOnlyList<CottonTransferQueueItem>> RestoreAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
