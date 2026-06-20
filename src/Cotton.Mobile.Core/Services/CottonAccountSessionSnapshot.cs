namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionSnapshot
    {
        public CottonAccountSessionSnapshot(
            string sessionId,
            string? device,
            string? ipAddress,
            string? userAgent,
            int authType,
            string? country,
            string? region,
            string? city,
            int refreshTokenCount,
            TimeSpan totalSessionDuration,
            bool isCurrentSession,
            DateTime lastSeenAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            if (refreshTokenCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshTokenCount));
            }

            if (totalSessionDuration < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(totalSessionDuration));
            }

            SessionId = sessionId.Trim();
            Device = NormalizeValue(device, "Unknown device");
            IpAddress = NormalizeValue(ipAddress, "Unknown IP");
            UserAgent = NormalizeValue(userAgent, "Unknown user agent");
            AuthType = authType;
            Country = NormalizeValue(country, "Unknown");
            Region = NormalizeValue(region, "Unknown");
            City = NormalizeValue(city, "Unknown");
            RefreshTokenCount = refreshTokenCount;
            TotalSessionDuration = totalSessionDuration;
            IsCurrentSession = isCurrentSession;
            LastSeenAt = NormalizeUtc(lastSeenAt);
        }

        public string SessionId { get; }

        public string Device { get; }

        public string IpAddress { get; }

        public string UserAgent { get; }

        public int AuthType { get; }

        public string Country { get; }

        public string Region { get; }

        public string City { get; }

        public int RefreshTokenCount { get; }

        public TimeSpan TotalSessionDuration { get; }

        public bool IsCurrentSession { get; }

        public DateTime LastSeenAt { get; }

        private static string NormalizeValue(string? value, string fallback)
        {
            string? normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            return value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
