// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IFileInteractionService
    {
        Task OpenAsync(CottonFileDownloadResult file, CancellationToken cancellationToken = default);

        Task ShareAsync(CottonFileDownloadResult file, CancellationToken cancellationToken = default);

        Task ShareAsync(IReadOnlyList<CottonFileDownloadResult> files, CancellationToken cancellationToken = default);
    }
}
