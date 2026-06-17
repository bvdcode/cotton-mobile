namespace Cotton.Mobile.Services
{
    public interface IFeedbackService
    {
        Task<bool> OpenFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default);
    }
}
