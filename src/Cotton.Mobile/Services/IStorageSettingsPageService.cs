namespace Cotton.Mobile.Services
{
    public interface IStorageSettingsPageService
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
    }
}
