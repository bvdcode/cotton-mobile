namespace Cotton.Mobile.Services
{
    public interface ICottonLogoutCacheCleanupSettingsStore
    {
        Task<CottonLogoutCacheCleanupSettings> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(
            CottonLogoutCacheCleanupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
