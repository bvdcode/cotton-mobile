namespace Cotton.Mobile.Services
{
    public interface IFileVersionHistoryPageService
    {
        Task OpenAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default);
    }
}
