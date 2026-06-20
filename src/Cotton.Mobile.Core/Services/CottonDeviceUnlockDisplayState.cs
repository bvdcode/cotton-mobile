namespace Cotton.Mobile.Services
{
    public class CottonDeviceUnlockDisplayState
    {
        private CottonDeviceUnlockDisplayState(
            CottonDeviceUnlockAvailabilitySnapshot availability,
            CottonDeviceUnlockResult? lastResult)
        {
            Availability = availability;
            LastResult = lastResult;
            StatusText = lastResult?.StatusText ?? availability.StatusText;
            DetailText = lastResult?.DetailText ?? availability.DetailText;
            CanVerify = availability.CanVerify;
        }

        public CottonDeviceUnlockAvailabilitySnapshot Availability { get; }

        public CottonDeviceUnlockResult? LastResult { get; }

        public string Title => "Device unlock";

        public string ActionText => "Verify unlock";

        public string StatusText { get; }

        public string DetailText { get; }

        public bool CanVerify { get; }

        public bool IsActionVisible => Availability.CanVerify;

        public static CottonDeviceUnlockDisplayState Create(
            CottonDeviceUnlockAvailabilitySnapshot availability,
            CottonDeviceUnlockResult? lastResult = null)
        {
            ArgumentNullException.ThrowIfNull(availability);

            return new CottonDeviceUnlockDisplayState(availability, lastResult);
        }
    }
}
