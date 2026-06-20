namespace Cotton.Mobile.Services
{
    public interface ISecuritySettingsPageService
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
    }
}
