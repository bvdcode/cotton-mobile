using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonTrashBrowserService : ICottonTrashBrowserService
    {
        private const int PageSize = 100;
        private const int MaxPageCount = 1000;
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
            int totalPages = CreateTotalPages(firstPage.TotalCount);
            int previousPageItemCount = CountPageItems(firstPage);
            for (int page = 2; ShouldLoadNextPage(page, totalPages, previousPageItemCount); page++)
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
                previousPageItemCount = CountPageItems(content);
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

        private static int CountPageItems(NodeContentDto content)
        {
            return content.Nodes.Count + content.Files.Count;
        }

        private static int CreateTotalPages(int totalCount)
        {
            if (totalCount <= 0)
            {
                return 1;
            }

            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        }

        private static bool ShouldLoadNextPage(
            int page,
            int totalPages,
            int previousPageItemCount)
        {
            if (page > MaxPageCount)
            {
                throw new InvalidOperationException("Trash contains too many pages to load safely.");
            }

            if (previousPageItemCount >= PageSize)
            {
                return true;
            }

            return page == 2 && page <= totalPages;
        }
    }
}
