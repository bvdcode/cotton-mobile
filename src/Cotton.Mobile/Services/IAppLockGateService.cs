namespace Cotton.Mobile.Services
{
    public interface IAppLockGateService
    {
        Task<CottonDeviceUnlockResult> ShowAndUnlockAsync(
            CancellationToken cancellationToken = default);
    }
}
