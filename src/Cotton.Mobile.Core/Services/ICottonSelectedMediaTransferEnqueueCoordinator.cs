// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonSelectedMediaTransferEnqueueCoordinator
    {
        Task<CottonSelectedMediaTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CottonFolderHandle destinationFolder,
            string? destinationPath,
            IReadOnlyList<CottonFileUploadSource> sources,
            CancellationToken cancellationToken = default);
    }
}
