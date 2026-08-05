// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonFileDownloadService
    {
        Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress,
            CancellationToken cancellationToken);

        CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file);

        CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(Uri instanceUri, CottonFileBrowserEntry file);

        CottonFileDownloadResult? GetReusableLocalDownload(Uri instanceUri, CottonFileBrowserEntry file);

        Task<bool> DeleteLocalDownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken);
    }
}
