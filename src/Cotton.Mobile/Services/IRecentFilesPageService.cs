namespace Cotton.Mobile.Services
{
    public interface IRecentFilesPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
