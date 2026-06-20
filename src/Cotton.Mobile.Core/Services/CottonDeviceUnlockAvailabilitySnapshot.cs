namespace Cotton.Mobile.Services
{
    public class CottonDeviceUnlockAvailabilitySnapshot
    {
        private CottonDeviceUnlockAvailabilitySnapshot(
            CottonDeviceUnlockAvailabilityKind availability,
            string statusText,
            string detailText)
        {
            if (!Enum.IsDefined(availability))
            {
                throw new ArgumentOutOfRangeException(nameof(availability), "Device unlock availability is unknown.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Device unlock status is required.", nameof(statusText));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Device unlock detail is required.", nameof(detailText));
            }

            Availability = availability;
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
        }

        public CottonDeviceUnlockAvailabilityKind Availability { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool CanVerify => Availability == CottonDeviceUnlockAvailabilityKind.Available;

        public static CottonDeviceUnlockAvailabilitySnapshot Available { get; } = new(
            CottonDeviceUnlockAvailabilityKind.Available,
            "Available",
            "Use the device screen lock to verify access.");

        public static CottonDeviceUnlockAvailabilitySnapshot Unavailable(string detailText)
        {
            return new CottonDeviceUnlockAvailabilitySnapshot(
                CottonDeviceUnlockAvailabilityKind.Unavailable,
                "Unavailable",
                detailText);
        }
    }
}
