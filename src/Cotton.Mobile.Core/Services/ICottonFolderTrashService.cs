namespace Cotton.Mobile.Services
{
    public interface ICottonFolderTrashService
    {
        Task<CottonFolderTrashMoveResult> MoveFolderToTrashAsync(
            Uri instanceUri,
            CottonFileBrowserEntry folder,
            CancellationToken cancellationToken = default);
    }
}
