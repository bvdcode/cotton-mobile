namespace Cotton.Mobile.Services
{
    public class CottonDeviceUnlockResult
    {
        private CottonDeviceUnlockResult(
            CottonDeviceUnlockResultKind result,
            string statusText,
            string detailText)
        {
            if (!Enum.IsDefined(result))
            {
                throw new ArgumentOutOfRangeException(nameof(result), "Device unlock result is unknown.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Device unlock result status is required.", nameof(statusText));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Device unlock result detail is required.", nameof(detailText));
            }

            Result = result;
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
        }

        public CottonDeviceUnlockResultKind Result { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool IsSucceeded => Result == CottonDeviceUnlockResultKind.Succeeded;

        public static CottonDeviceUnlockResult Succeeded { get; } = new(
            CottonDeviceUnlockResultKind.Succeeded,
            "Verified",
            "Device unlock was confirmed.");

        public static CottonDeviceUnlockResult Cancelled { get; } = new(
            CottonDeviceUnlockResultKind.Cancelled,
            "Cancelled",
            "Device unlock was not confirmed.");

        public static CottonDeviceUnlockResult Unavailable(string detailText)
        {
            return new CottonDeviceUnlockResult(
                CottonDeviceUnlockResultKind.Unavailable,
                "Unavailable",
                detailText);
        }

        public static CottonDeviceUnlockResult Failed(string detailText)
        {
            return new CottonDeviceUnlockResult(
                CottonDeviceUnlockResultKind.Failed,
                "Failed",
                detailText);
        }
    }
}
