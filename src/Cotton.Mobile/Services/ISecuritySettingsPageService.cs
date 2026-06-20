namespace Cotton.Mobile.Services
{
    public interface ISecuritySettingsPageService
    {
        Task OpenAsync(
            ICottonCurrentSessionRevocationHandler revocationHandler,
            CancellationToken cancellationToken = default);
    }
}
