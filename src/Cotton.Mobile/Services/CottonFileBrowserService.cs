// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Nodes;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserService : ICottonFileBrowserService
    {
        private const int PageSize = 100;

        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonFileDownloadService _downloadService;

        public CottonFileBrowserService(
            ICottonClientFactory clientFactory,
            ICottonFileDownloadService downloadService)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(downloadService);

            _clientFactory = clientFactory;
            _downloadService = downloadService;
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

        public async Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonFolderHandle parentFolder,
            string folderName,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(parentFolder);
            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Folder name is required.", nameof(folderName));
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            NodeDto node = await client.Nodes
                .CreateAsync(parentFolder.Id, folderName.Trim(), cancellationToken)
                .ConfigureAwait(false);
            return CottonFileBrowserEntry.FromNode(node);
        }

        public Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return _downloadService.DownloadAsync(instanceUri, file, progress, cancellationToken);
        }

        public CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file)
        {
            return _downloadService.GetLocalDownload(instanceUri, file);
        }

        public CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            return _downloadService.GetReusableLocalDownloadSnapshot(instanceUri, file);
        }

        public CottonFileDownloadResult? GetReusableLocalDownload(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            return _downloadService.GetReusableLocalDownload(instanceUri, file);
        }

        public Task<bool> DeleteLocalDownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default)
        {
            return _downloadService.DeleteLocalDownloadAsync(instanceUri, file, cancellationToken);
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
    }
}
