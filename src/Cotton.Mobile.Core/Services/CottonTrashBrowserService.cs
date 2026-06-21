using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonTrashBrowserService : ICottonTrashBrowserService
    {
        private const int PageSize = 100;
        private const string TrashFolderName = "Trash";

        private readonly ICottonTrashBrowserClient _client;

        public CottonTrashBrowserService(ICottonTrashBrowserClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonFolderContent> GetRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            NodeDto root = await _client.GetTrashRootAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            NodeContentDto firstPage = await _client.GetChildrenAsync(
                    instanceUri,
                    root.Id,
                    page: 1,
                    pageSize: PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

            var nodes = new List<NodeDto>(firstPage.Nodes);
            var files = new List<NodeFileManifestDto>(firstPage.Files);
            int totalPages = (int)Math.Ceiling(firstPage.TotalCount / (double)PageSize);
            for (int page = 2; page <= totalPages; page++)
            {
                NodeContentDto content = await _client.GetChildrenAsync(
                        instanceUri,
                        root.Id,
                        page,
                        PageSize,
                        cancellationToken)
                    .ConfigureAwait(false);
                nodes.AddRange(content.Nodes);
                files.AddRange(content.Files);
            }

            List<CottonFileBrowserEntry> entries = nodes
                .Select(CottonFileBrowserEntry.FromNode)
                .Concat(files.Select(CottonFileBrowserEntry.FromFile))
                .OrderByDescending(entry => entry.UpdatedAtUtc)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Id)
                .ToList();

            return new CottonFolderContent(root.Id, CreateFolderName(root), entries);
        }

        private static string CreateFolderName(NodeDto root)
        {
            return string.IsNullOrWhiteSpace(root.Name)
                ? TrashFolderName
                : root.Name.Trim();
        }
    }
}
