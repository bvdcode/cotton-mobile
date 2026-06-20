namespace Cotton.Mobile.Services
{
    public interface ICottonAppLockSettingsStore
    {
        Task<CottonAppLockSettings> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(CottonAppLockSettings settings, CancellationToken cancellationToken = default);
    }
}
