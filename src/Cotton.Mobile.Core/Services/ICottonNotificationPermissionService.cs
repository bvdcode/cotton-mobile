namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationPermissionService
    {
        Task<CottonNotificationPermissionState> GetPermissionStateAsync(
            CancellationToken cancellationToken = default);

        Task<CottonNotificationPermissionState> RequestPermissionAsync(
            CancellationToken cancellationToken = default);

        Task OpenSettingsAsync(CancellationToken cancellationToken = default);
    }
}
