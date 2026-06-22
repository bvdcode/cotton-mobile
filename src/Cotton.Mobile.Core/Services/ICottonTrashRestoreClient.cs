// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public interface ICottonTrashRestoreClient
    {
        Task<RestoreOutcomeDto> RestoreFileAsync(
            Uri instanceUri,
            Guid fileId,
            RestoreItemRequestDto request,
            CancellationToken cancellationToken = default);

        Task<RestoreOutcomeDto> RestoreFolderAsync(
            Uri instanceUri,
            Guid folderId,
            RestoreItemRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
