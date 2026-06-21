using Cotton.Files;
using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonApiTrashRestoreClient : ICottonTrashRestoreClient
    {
        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonApiTrashRestoreClient(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
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

            string path = $"{Routes.V1.Files}/{fileId}/restore";
            return await _apiClient
                .SendJsonAsync<RestoreOutcomeDto>(instanceUri, HttpMethod.Post, path, request, cancellationToken)
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

            string path = $"{Routes.V1.Layouts}/nodes/{folderId}/restore";
            return await _apiClient
                .SendJsonAsync<RestoreOutcomeDto>(instanceUri, HttpMethod.Post, path, request, cancellationToken)
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
