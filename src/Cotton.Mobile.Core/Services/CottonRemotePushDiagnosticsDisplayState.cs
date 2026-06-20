using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDiagnosticsDisplayState
    {
        private CottonRemotePushDiagnosticsDisplayState(
            string providerText,
            string platformText,
            string backendText,
            string platformTokenText,
            string sessionRegistrationText,
            string lastAttemptText,
            string reasonText)
        {
            ProviderText = providerText;
            PlatformText = platformText;
            BackendText = backendText;
            PlatformTokenText = platformTokenText;
            SessionRegistrationText = sessionRegistrationText;
            LastAttemptText = lastAttemptText;
            ReasonText = reasonText;
        }

        public string ProviderText { get; }

        public string PlatformText { get; }

        public string BackendText { get; }

        public string PlatformTokenText { get; }

        public string SessionRegistrationText { get; }

        public string LastAttemptText { get; }

        public string ReasonText { get; }

        public static CottonRemotePushDiagnosticsDisplayState Create(
            CottonRemotePushDiagnosticsSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CottonRemotePushDiagnosticsDisplayState(
                FormatProvider(snapshot.Capability.Provider),
                FormatPlatform(snapshot.Capability.Platform),
                FormatBackend(snapshot.Capability),
                FormatPlatformToken(snapshot.PlatformToken),
                FormatRegistrationStatus(snapshot.LastRegistrationStatus),
                FormatLastAttempt(snapshot.LastRegistrationAttemptedAtUtc),
                string.IsNullOrWhiteSpace(snapshot.PlatformToken.StatusReason)
                    ? "Not available"
                    : snapshot.PlatformToken.StatusReason);
        }

        private static string FormatProvider(CottonRemotePushProviderKind provider)
        {
            return provider switch
            {
                CottonRemotePushProviderKind.FirebaseCloudMessaging => "Firebase Cloud Messaging",
                _ => provider.ToString(),
            };
        }

        private static string FormatPlatform(CottonRemotePushMobilePlatform platform)
        {
            return platform switch
            {
                CottonRemotePushMobilePlatform.Android => "Android",
                CottonRemotePushMobilePlatform.Ios => "iOS",
                _ => platform.ToString(),
            };
        }

        private static string FormatBackend(CottonRemotePushCapabilitySnapshot capability)
        {
            if (capability.CanDeliverRemotePush)
            {
                return "Ready";
            }

            return capability.MissingServerCapabilities.Count == 1
                ? "Missing 1 server capability"
                : $"Missing {capability.MissingServerCapabilities.Count:N0} server capabilities";
        }

        private static string FormatPlatformToken(CottonRemotePushPlatformTokenSnapshot token)
        {
            return token.Status switch
            {
                CottonRemotePushPlatformTokenStatus.Available => "Available",
                CottonRemotePushPlatformTokenStatus.NotConfigured => "Not configured",
                CottonRemotePushPlatformTokenStatus.Unavailable => "Unavailable",
                _ => token.Status.ToString(),
            };
        }

        private static string FormatRegistrationStatus(CottonRemotePushRegistrationStatus? status)
        {
            return status switch
            {
                CottonRemotePushRegistrationStatus.Registered => "Registered",
                CottonRemotePushRegistrationStatus.NotConfigured => "Skipped, not configured",
                CottonRemotePushRegistrationStatus.Unavailable => "Skipped, unavailable",
                null => "Not attempted",
                _ => status.Value.ToString(),
            };
        }

        private static string FormatLastAttempt(DateTimeOffset? attemptedAtUtc)
        {
            return attemptedAtUtc.HasValue
                ? attemptedAtUtc.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "Not available";
        }
    }
}
