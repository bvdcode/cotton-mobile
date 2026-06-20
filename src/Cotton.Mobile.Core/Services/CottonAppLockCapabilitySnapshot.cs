namespace Cotton.Mobile.Services
{
    public class CottonAppLockCapabilitySnapshot
    {
        private CottonAppLockCapabilitySnapshot(
            CottonAppLockAvailabilityKind availability,
            string statusText,
            string detailText)
        {
            if (!Enum.IsDefined(availability))
            {
                throw new ArgumentOutOfRangeException(nameof(availability), "App lock availability is unknown.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("App lock status is required.", nameof(statusText));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("App lock detail is required.", nameof(detailText));
            }

            Availability = availability;
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
        }

        public CottonAppLockAvailabilityKind Availability { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool CanEnable => Availability == CottonAppLockAvailabilityKind.Available;

        public static CottonAppLockCapabilitySnapshot Available { get; } = new(
            CottonAppLockAvailabilityKind.Available,
            "Available",
            "Device unlock can protect Cotton.");

        public static CottonAppLockCapabilitySnapshot Unavailable(string detailText)
        {
            return new CottonAppLockCapabilitySnapshot(
                CottonAppLockAvailabilityKind.Unavailable,
                "Unavailable",
                detailText);
        }
    }
}
