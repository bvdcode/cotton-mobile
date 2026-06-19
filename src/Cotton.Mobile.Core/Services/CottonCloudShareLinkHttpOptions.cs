namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkHttpOptions
    {
        public CottonCloudShareLinkHttpOptions(
            string? userAgent,
            string? deviceName,
            bool refreshOnUnauthorized = true)
        {
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
            RefreshOnUnauthorized = refreshOnUnauthorized;
        }

        public static CottonCloudShareLinkHttpOptions Default { get; } =
            new(userAgent: null, deviceName: null);

        public string? UserAgent { get; }

        public string? DeviceName { get; }

        public bool RefreshOnUnauthorized { get; }
    }
}
