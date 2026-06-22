// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonQueuedUploadExecutor
    {
        Task<CottonQueuedUploadExecutionResult> ExecuteNextAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonQueuedUploadExecutionResult> ExecuteAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);
    }
}
