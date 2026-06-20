namespace Cotton.Mobile.Services
{
    public interface ICottonAppLockRuntimeStateStore
    {
        Task<CottonAppLockRuntimeState> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(
            CottonAppLockRuntimeState runtimeState,
            CancellationToken cancellationToken = default);
    }
}
