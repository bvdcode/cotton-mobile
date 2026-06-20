namespace Cotton.Mobile.Services
{
    public class DeviceUnlockCottonAppLockCapabilityService : ICottonAppLockCapabilityService
    {
        private readonly ICottonDeviceUnlockService _deviceUnlockService;

        public DeviceUnlockCottonAppLockCapabilityService(ICottonDeviceUnlockService deviceUnlockService)
        {
            ArgumentNullException.ThrowIfNull(deviceUnlockService);

            _deviceUnlockService = deviceUnlockService;
        }

        public async Task<CottonAppLockCapabilitySnapshot> GetCapabilityAsync(
            CancellationToken cancellationToken = default)
        {
            CottonDeviceUnlockAvailabilitySnapshot availability =
                await _deviceUnlockService.GetAvailabilityAsync(cancellationToken).ConfigureAwait(false);
            return availability.CanVerify
                ? CottonAppLockCapabilitySnapshot.Available
                : CottonAppLockCapabilitySnapshot.Unavailable(availability.DetailText);
        }
    }
}
