namespace Cotton.Mobile.Services
{
    public interface ICottonFolderTrashClient
    {
        Task MoveFolderToTrashAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default);
    }
}
