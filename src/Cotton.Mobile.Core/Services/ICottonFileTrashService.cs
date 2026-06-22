// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonFileTrashService
    {
        Task<CottonFileTrashMoveResult> MoveFileToTrashAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default);
    }
}
