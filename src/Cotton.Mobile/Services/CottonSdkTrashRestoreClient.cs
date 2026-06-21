using Cotton.Files;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonSdkTrashRestoreClient : ICottonTrashRestoreClient
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonSdkTrashRestoreClient(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
        }

        public async Task<RestoreOutcomeDto> RestoreFileAsync(
            Uri instanceUri,
            Guid fileId,
            RestoreItemRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ValidateItemId(fileId, nameof(fileId));
            ArgumentNullException.ThrowIfNull(request);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            return await client.Files
                .RestoreAsync(fileId, request, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RestoreOutcomeDto> RestoreFolderAsync(
            Uri instanceUri,
            Guid folderId,
            RestoreItemRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ValidateItemId(folderId, nameof(folderId));
            ArgumentNullException.ThrowIfNull(request);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            return await client.Nodes
                .RestoreAsync(folderId, request, cancellationToken)
                .ConfigureAwait(false);
        }

        private static void ValidateItemId(Guid itemId, string parameterName)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Restore item id is required.", parameterName);
            }
        }
    }
}
