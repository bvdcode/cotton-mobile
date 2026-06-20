using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemotePushDiagnosticsDisplayTests
    {
        [Fact]
        public void Create_FormatsAvailableTokenWithoutExposingToken()
        {
            var snapshot = new CottonRemotePushDiagnosticsSnapshot(
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend,
                CottonRemotePushPlatformTokenSnapshot.Available(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "secret-token"),
                CottonRemotePushRegistrationStatus.Registered,
                new DateTimeOffset(2026, 6, 19, 23, 59, 0, TimeSpan.Zero));

            CottonRemotePushDiagnosticsDisplayState state =
                CottonRemotePushDiagnosticsDisplayState.Create(snapshot);

            Assert.Equal("Firebase Cloud Messaging", state.ProviderText);
            Assert.Equal("Android", state.PlatformText);
            Assert.Equal("Ready", state.BackendText);
            Assert.Equal("Available", state.PlatformTokenText);
            Assert.Equal("Registered", state.SessionRegistrationText);
            Assert.Equal("2026-06-19 23:59:00 UTC", state.LastAttemptText);
            Assert.Equal("Not available", state.ReasonText);
            Assert.DoesNotContain("secret-token", state.ProviderText);
            Assert.DoesNotContain("secret-token", state.PlatformTokenText);
            Assert.DoesNotContain("secret-token", state.ReasonText);
        }

        [Fact]
        public void Create_FormatsNotConfiguredTokenReason()
        {
            var snapshot = new CottonRemotePushDiagnosticsSnapshot(
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend,
                CottonRemotePushPlatformTokenSnapshot.NotConfigured(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Firebase config file is missing."),
                lastRegistrationStatus: null,
                lastRegistrationAttemptedAtUtc: null);

            CottonRemotePushDiagnosticsDisplayState state =
                CottonRemotePushDiagnosticsDisplayState.Create(snapshot);

            Assert.Equal("Not configured", state.PlatformTokenText);
            Assert.Equal("Not attempted", state.SessionRegistrationText);
            Assert.Equal("Not available", state.LastAttemptText);
            Assert.Equal("Firebase config file is missing.", state.ReasonText);
        }
    }
}
