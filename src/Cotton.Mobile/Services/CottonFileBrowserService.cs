using Cotton.Files;
using Cotton.Nodes;
using Cotton.Sdk;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserService : ICottonFileBrowserService
    {
        private const int PageSize = 100;

        private readonly ICottonClientFactory _clientFactory;
        private readonly IFileDownloadCachePruner _downloadCachePruner;
        private readonly ILogger<CottonFileBrowserService> _logger;

        public CottonFileBrowserService(
            ICottonClientFactory clientFactory,
            IFileDownloadCachePruner downloadCachePruner,
            ILogger<CottonFileBrowserService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(downloadCachePruner);
            ArgumentNullException.ThrowIfNull(logger);

            _clientFactory = clientFactory;
            _downloadCachePruner = downloadCachePruner;
            _logger = logger;
        }

        public async Task<CottonFolderContent> GetRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            NodeDto root = await client.Nodes.ResolveAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return await LoadFolderAsync(client, root.Id, root.Name, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CottonFolderContent> GetFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(folder);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            return await LoadFolderAsync(client, folder.Id, folder.Name, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Only files can be downloaded.", nameof(file));
            }

            string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, file);
            Directory.CreateDirectory(directory);
            string filePath = CottonMobileStoragePaths.CreateDownloadPath(instanceUri, file);
            string tempFilePath = filePath + ".download";
            long sizeBytes = 0;
            bool tempFileReady = false;

            try
            {
                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                await using var destination = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);
                Stream downloadTarget = progress is null
                    ? destination
                    : new ProgressReportingStream(destination, progress);
                await client.Files.DownloadContentAsync(file.Id, downloadTarget, download: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                sizeBytes = destination.Length;
                tempFileReady = true;
            }
            finally
            {
                if (!tempFileReady)
                {
                    DeleteTemporaryDownload(tempFilePath);
                }
            }

            try
            {
                File.Move(tempFilePath, filePath, overwrite: true);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to replace Cotton mobile download file {Path}.", filePath);
                DeleteTemporaryDownload(tempFilePath);
                throw;
            }

            await _downloadCachePruner.PruneAsync(filePath, CancellationToken.None).ConfigureAwait(false);

            return new CottonFileDownloadResult(file.Name, filePath, sizeBytes);
        }

        public CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            FileInfo? info = GetLocalDownloadFile(instanceUri, file);
            if (info is null)
            {
                return null;
            }

            return CreateLocalFileSnapshot(info);
        }

        public CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            FileInfo? info = GetReusableLocalDownloadFile(instanceUri, file);
            return info is null ? null : CreateLocalFileSnapshot(info);
        }

        public CottonFileDownloadResult? GetReusableLocalDownload(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            FileInfo? info = GetReusableLocalDownloadFile(instanceUri, file);
            if (info is null)
            {
                return null;
            }

            TouchLocalDownload(info);
            return new CottonFileDownloadResult(file.Name, info.FullName, info.Length);
        }

        private static async Task<CottonFolderContent> LoadFolderAsync(
            ICottonCloudClient client,
            Guid folderId,
            string folderName,
            CancellationToken cancellationToken)
        {
            NodeContentDto firstPage = await client.Nodes.GetChildrenAsync(
                folderId,
                page: 1,
                pageSize: PageSize,
                depth: 0,
                cancellationToken).ConfigureAwait(false);

            var nodes = new List<NodeDto>(firstPage.Nodes);
            var files = new List<NodeFileManifestDto>(firstPage.Files);
            int totalPages = (int)Math.Ceiling(firstPage.TotalCount / (double)PageSize);

            for (int page = 2; page <= totalPages; page++)
            {
                NodeContentDto content = await client.Nodes.GetChildrenAsync(
                    folderId,
                    page,
                    PageSize,
                    depth: 0,
                    cancellationToken).ConfigureAwait(false);
                nodes.AddRange(content.Nodes);
                files.AddRange(content.Files);
            }

            List<CottonFileBrowserEntry> entries = nodes
                .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
                .Select(CottonFileBrowserEntry.FromNode)
                .Concat(
                    files
                        .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(CottonFileBrowserEntry.FromFile))
                .ToList();

            return new CottonFolderContent(folderId, folderName, entries);
        }

        private static FileInfo? GetLocalDownloadFile(Uri instanceUri, CottonFileBrowserEntry file)
        {
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                return null;
            }

            var info = new FileInfo(CottonMobileStoragePaths.CreateDownloadPath(instanceUri, file));
            return info.Exists ? info : null;
        }

        private static FileInfo? GetReusableLocalDownloadFile(Uri instanceUri, CottonFileBrowserEntry file)
        {
            FileInfo? info = GetLocalDownloadFile(instanceUri, file);
            if (info is null || file.SizeBytes != info.Length)
            {
                return null;
            }

            return info;
        }

        private static CottonLocalFileSnapshot CreateLocalFileSnapshot(FileInfo info)
        {
            return new CottonLocalFileSnapshot(info.Name, info.Length, info.LastWriteTimeUtc);
        }

        private void TouchLocalDownload(FileInfo info)
        {
            try
            {
                info.LastWriteTimeUtc = DateTime.UtcNow;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to update Cotton mobile local file timestamp {Path}.", info.FullName);
            }
        }

        private void DeleteTemporaryDownload(string tempFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to delete temporary Cotton mobile download file {Path}.", tempFilePath);
            }
        }
    }
}
