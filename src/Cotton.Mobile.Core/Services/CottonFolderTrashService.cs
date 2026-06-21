namespace Cotton.Mobile.Services
{
    public class CottonFolderTrashService : ICottonFolderTrashService
    {
        private readonly ICottonFolderTrashClient _client;

        public CottonFolderTrashService(ICottonFolderTrashClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonFolderTrashMoveResult> MoveFolderToTrashAsync(
            Uri instanceUri,
            CottonFileBrowserEntry folder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(folder);
            if (folder.Type != CottonFileBrowserEntryType.Folder)
            {
                throw new ArgumentException("Only folders can be moved to trash.", nameof(folder));
            }

            await _client.MoveFolderToTrashAsync(
                instanceUri,
                folder.Id,
                cancellationToken).ConfigureAwait(false);
            return CottonFolderTrashMoveResult.Moved(folder);
        }
    }
}
