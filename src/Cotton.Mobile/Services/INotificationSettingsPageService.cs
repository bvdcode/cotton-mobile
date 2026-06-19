namespace Cotton.Mobile.Services
{
    public interface INotificationSettingsPageService
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
    }
}
