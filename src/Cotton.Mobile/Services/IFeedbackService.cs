namespace Cotton.Mobile.Services
{
    public interface IFeedbackService
    {
        Task<bool> OpenFeedbackAsync(
            string? instanceUrl,
            string? profileName,
            CancellationToken cancellationToken = default);
    }
}
