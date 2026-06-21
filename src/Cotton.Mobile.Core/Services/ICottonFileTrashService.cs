namespace Cotton.Mobile.Services
{
    public interface ICottonFileTrashService
    {
        Task<CottonFileTrashMoveResult> MoveFileToTrashAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default);
    }
}
