namespace Cotton.Mobile.Services
{
    public interface ITransfersPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
