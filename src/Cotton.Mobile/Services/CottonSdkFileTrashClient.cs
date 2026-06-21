using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonSdkFileTrashClient : ICottonFileTrashClient
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonSdkFileTrashClient(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
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

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await client.Files
                .DeleteAsync(fileId, skipTrash: false, expectedETag.Trim(), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
