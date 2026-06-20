namespace Cotton.Mobile.Services
{
    public interface ISyncSettingsPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
