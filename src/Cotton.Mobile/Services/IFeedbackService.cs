namespace Cotton.Mobile.Services
{
    public interface IFeedbackService
    {
        Task<FeedbackDeliveryResult> OpenFeedbackAsync(
            FeedbackContext context,
            CancellationToken cancellationToken = default);

        Task CopyFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default);
    }
}
