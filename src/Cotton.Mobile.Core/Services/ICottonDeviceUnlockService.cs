namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceUnlockService
    {
        Task<CottonDeviceUnlockAvailabilitySnapshot> GetAvailabilityAsync(
            CancellationToken cancellationToken = default);

        Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            CancellationToken cancellationToken = default);
    }
}
