using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonSdkFolderTrashClient : ICottonFolderTrashClient
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonSdkFolderTrashClient(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
        }

        public async Task MoveFolderToTrashAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await client.Nodes
                .DeleteAsync(folderId, skipTrash: false, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
