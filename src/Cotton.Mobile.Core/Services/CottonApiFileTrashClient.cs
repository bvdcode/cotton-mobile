using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonApiFileTrashClient : ICottonFileTrashClient
    {
        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonApiFileTrashClient(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task MoveFileToTrashAsync(
            Uri instanceUri,
            Guid fileId,
            string expectedETag,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(expectedETag))
            {
                throw new ArgumentException("Expected file ETag is required.", nameof(expectedETag));
            }

            string path = Routes.V1.Files
                + "/"
                + fileId
                + "?skipTrash=false&expectedETag="
                + Uri.EscapeDataString(expectedETag.Trim());
            await _apiClient
                .SendRequiredAsync(instanceUri, HttpMethod.Delete, path, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
