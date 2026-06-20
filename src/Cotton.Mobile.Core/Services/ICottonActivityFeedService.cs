namespace Cotton.Mobile.Services
{
    public interface ICottonActivityFeedService
    {
        Task<CottonActivityFeedPageSnapshot> GetPageAsync(
            Uri instanceUri,
            CottonActivityFeedQuery query,
            CancellationToken cancellationToken = default);
    }
}
