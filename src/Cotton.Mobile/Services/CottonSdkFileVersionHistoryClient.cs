using Cotton.Files;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonSdkFileVersionHistoryClient : ICottonFileVersionHistoryClient
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonSdkFileVersionHistoryClient(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
        }

        public async Task<IReadOnlyList<FileVersionDto>> GetVersionsAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            List<FileVersionDto> versions = await client.Files
                .GetVersionsAsync(fileId, cancellationToken)
                .ConfigureAwait(false);
            return versions;
        }
    }
}
