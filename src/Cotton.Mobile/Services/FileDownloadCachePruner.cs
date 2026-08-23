// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileDownloadCachePruner(
        FileDownloadCacheProtectionProvider protectionProvider,
        FileDownloadCacheFilePruner filePruner) : IFileDownloadCachePruner
    {
        private readonly FileDownloadCacheProtectionProvider _protectionProvider =
            protectionProvider ?? throw new ArgumentNullException(nameof(protectionProvider));
        private readonly FileDownloadCacheFilePruner _filePruner =
            filePruner ?? throw new ArgumentNullException(nameof(filePruner));

        public async Task PruneAsync(
            Uri instanceUri,
            string? protectedPath = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            IReadOnlyCollection<string> protectedDirectories = await _protectionProvider
                .LoadAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            await Task.Run(
                    () => _filePruner.Prune(protectedPath, protectedDirectories, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
