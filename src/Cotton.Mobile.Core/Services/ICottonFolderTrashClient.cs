// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonFolderTrashClient
    {
        Task MoveFolderToTrashAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default);
    }
}
