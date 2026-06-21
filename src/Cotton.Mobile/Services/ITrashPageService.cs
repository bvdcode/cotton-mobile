namespace Cotton.Mobile.Services
{
    public interface ITrashPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
