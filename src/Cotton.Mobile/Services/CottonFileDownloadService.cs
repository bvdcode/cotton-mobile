// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonFileDownloadService : ICottonFileDownloadService
    {
        private const int BufferSize = 81920;

        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonLocalDownloadCache _localCache;
        private readonly IFileDownloadCachePruner _downloadCachePruner;

        public CottonFileDownloadService(
            ICottonClientFactory clientFactory,
            ICottonLocalDownloadCache localCache,
            IFileDownloadCachePruner downloadCachePruner)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(localCache);
            ArgumentNullException.ThrowIfNull(downloadCachePruner);

            _clientFactory = clientFactory;
            _localCache = localCache;
            _downloadCachePruner = downloadCachePruner;
        }

        public async Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Only files can be downloaded.", nameof(file));
            }

            string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, file);
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(CottonMobileStoragePaths.CreateTemporaryDownloadsDirectory());
            string filePath = CottonMobileStoragePaths.CreateDownloadPath(instanceUri, file);
            string temporaryPath = CottonMobileStoragePaths.CreateTemporaryDownloadPath();
            long sizeBytes = 0;
            bool finalFileReady = false;
            try
            {
                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                await using (FileStream destination = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    useAsync: true))
                {
                    Stream downloadTarget = progress is null
                        ? destination
                        : new ProgressReportingStream(destination, progress);
                    await client.Files.DownloadContentAsync(
                        file.Id,
                        downloadTarget,
                        download: true,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    sizeBytes = destination.Length;
                }

                await ValidateDownloadedContentAsync(
                    file,
                    temporaryPath,
                    sizeBytes,
                    cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _localCache.CommitDownload(temporaryPath, filePath, directory, file);
                finalFileReady = true;

                await _downloadCachePruner.PruneAsync(instanceUri, filePath, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new CottonFileDownloadResult(file.Name, filePath, sizeBytes, file.ContentType);
            }
            finally
            {
                if (!finalFileReady)
                {
                    _localCache.DeleteTemporaryDownload(temporaryPath);
                }
            }
        }

        public CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file)
        {
            return _localCache.GetLocalDownload(instanceUri, file);
        }

        public CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            return _localCache.GetReusableLocalDownloadSnapshot(instanceUri, file);
        }

        public CottonFileDownloadResult? GetReusableLocalDownload(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            return _localCache.GetReusableLocalDownload(instanceUri, file);
        }

        public Task<bool> DeleteLocalDownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken)
        {
            return _localCache.DeleteLocalDownloadAsync(instanceUri, file, cancellationToken);
        }

        private static async Task ValidateDownloadedContentAsync(
            CottonFileBrowserEntry file,
            string filePath,
            long downloadedSizeBytes,
            CancellationToken cancellationToken)
        {
            if (file.SizeBytes.HasValue && downloadedSizeBytes != file.SizeBytes.Value)
            {
                throw new IOException(
                    $"Downloaded file size mismatch for {file.Id}: expected {file.SizeBytes.Value} bytes, got {downloadedSizeBytes} bytes.");
            }

            if (file.ContentHash is null)
            {
                throw new IOException($"Downloaded file manifest does not contain a content hash for {file.Id}.");
            }

            await using FileStream content = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true);
            string contentHash = await CottonContentHash.ComputeSha256Async(content, cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(contentHash, file.ContentHash, StringComparison.Ordinal))
            {
                throw new IOException($"Downloaded file content hash mismatch for {file.Id}.");
            }
        }
    }
}
