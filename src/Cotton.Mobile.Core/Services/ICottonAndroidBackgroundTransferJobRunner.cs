// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundTransferJobRunner
    {
        Task<CottonQueuedUploadExecutionResult> RunAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default);
    }
}
