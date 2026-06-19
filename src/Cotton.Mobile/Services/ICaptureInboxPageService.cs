namespace Cotton.Mobile.Services
{
    public interface ICaptureInboxPageService
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
    }
}
