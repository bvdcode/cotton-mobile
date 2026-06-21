using Cotton;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public class CottonApiTrashBrowserClient : ICottonTrashBrowserClient
    {
        private const string TrashNodeType = "Trash";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonApiTrashBrowserClient(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public Task<NodeDto> GetTrashRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            string path = $"{Routes.V1.Layouts}/resolver?nodeType={TrashNodeType}";
            return _apiClient.SendJsonAsync<NodeDto>(
                instanceUri,
                HttpMethod.Get,
                path,
                cancellationToken);
        }

        public Task<NodeContentDto> GetChildrenAsync(
            Uri instanceUri,
            Guid trashFolderId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (trashFolderId == Guid.Empty)
            {
                throw new ArgumentException("Trash folder id is required.", nameof(trashFolderId));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

            string path = $"{Routes.V1.Layouts}/nodes/{trashFolderId}/children"
                + $"?nodeType={TrashNodeType}&page={page}&pageSize={pageSize}&depth=1";
            return _apiClient.SendJsonAsync<NodeContentDto>(
                instanceUri,
                HttpMethod.Get,
                path,
                cancellationToken);
        }
    }
}
