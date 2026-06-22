// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public interface ICottonFileVersionHistoryClient
    {
        Task<IReadOnlyList<FileVersionDto>> GetVersionsAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default);
    }
}
