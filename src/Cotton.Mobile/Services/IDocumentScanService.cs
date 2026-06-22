// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IDocumentScanService
    {
        bool IsAvailable { get; }

        Task<CottonFileUploadSource?> ScanDocumentAsync(CancellationToken cancellationToken = default);
    }
}
