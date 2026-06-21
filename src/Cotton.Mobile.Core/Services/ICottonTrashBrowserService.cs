namespace Cotton.Mobile.Services
{
    public interface ICottonTrashBrowserService
    {
        Task<CottonFolderContent> GetRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
