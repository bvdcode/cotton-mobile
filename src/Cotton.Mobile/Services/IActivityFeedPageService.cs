namespace Cotton.Mobile.Services
{
    public interface IActivityFeedPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
