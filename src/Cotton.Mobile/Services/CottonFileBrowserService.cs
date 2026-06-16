using Cotton.Nodes;
using Cotton.Sdk;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserService : ICottonFileBrowserService
    {
        private const int PageSize = 100;
        private const string DownloadDirectoryName = "CottonDownloads";

        private readonly ICottonClientFactory _clientFactory;

        public CottonFileBrowserService(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
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
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Only files can be downloaded.", nameof(file));
            }

            string directory = Path.Combine(FileSystem.AppDataDirectory, DownloadDirectoryName);
            Directory.CreateDirectory(directory);
            string filePath = CreateAvailablePath(directory, file.Name);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await using var destination = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await client.Files.DownloadContentAsync(file.Id, destination, download: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new CottonFileDownloadResult(file.Name, filePath, destination.Length);
        }

        private static async Task<CottonFolderContent> LoadFolderAsync(
            ICottonCloudClient client,
            Guid folderId,
            string folderName,
            CancellationToken cancellationToken)
        {
            NodeContentDto content = await client.Nodes.GetChildrenAsync(
                folderId,
                page: 1,
                pageSize: PageSize,
                depth: 0,
                cancellationToken).ConfigureAwait(false);

            List<CottonFileBrowserEntry> entries = content.Nodes
                .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
                .Select(CottonFileBrowserEntry.FromNode)
                .Concat(
                    content.Files
                        .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(CottonFileBrowserEntry.FromFile))
                .ToList();

            return new CottonFolderContent(folderId, folderName, entries);
        }

        private static string CreateAvailablePath(string directory, string fileName)
        {
            string safeFileName = CreateSafeFileName(fileName);
            string candidate = Path.Combine(directory, safeFileName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            string name = Path.GetFileNameWithoutExtension(safeFileName);
            string extension = Path.GetExtension(safeFileName);
            for (int index = 1; index < int.MaxValue; index++)
            {
                candidate = Path.Combine(directory, $"{name} ({index}){extension}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new IOException("Could not create a unique download file name.");
        }

        private static string CreateSafeFileName(string fileName)
        {
            string trimmedName = string.IsNullOrWhiteSpace(fileName) ? "download" : fileName.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var buffer = new char[trimmedName.Length];
            for (int index = 0; index < trimmedName.Length; index++)
            {
                char character = trimmedName[index];
                buffer[index] = invalidChars.Contains(character) ? '_' : character;
            }

            return new string(buffer);
        }
    }
}
