using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemotePushRegistrationCoordinatorTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid TokenId = Guid.Parse("33333333-4444-5555-6666-777777777777");
        private static readonly DateTime RegisteredAt =
            new(2026, 6, 19, 23, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task RegisterCurrentAsync_registers_available_platform_token()
        {
            var platformTokenProvider = new FakePlatformTokenProvider(
                CottonRemotePushPlatformTokenSnapshot.Available(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    " fcm-token "));
            var deviceTokenService = new FakeDeviceTokenService(CreateDeviceTokenSnapshot());
            var coordinator = new CottonRemotePushRegistrationCoordinator(
                platformTokenProvider,
                deviceTokenService);

            CottonRemotePushRegistrationResult result = await coordinator.RegisterCurrentAsync(
                InstanceUri,
                " Pixel 8 ",
                " 1.2.3 ");

            Assert.Equal(CottonRemotePushRegistrationStatus.Registered, result.Status);
            Assert.Same(deviceTokenService.Response, result.DeviceToken);
            Assert.Single(deviceTokenService.RegistrationCalls);
            RegistrationCall call = deviceTokenService.RegistrationCalls.Single();
            Assert.Equal(InstanceUri, call.InstanceUri);
            Assert.Equal(CottonRemotePushProviderKind.FirebaseCloudMessaging, call.Request.Provider);
            Assert.Equal(CottonRemotePushMobilePlatform.Android, call.Request.Platform);
            Assert.Equal("fcm-token", call.Request.Token);
            Assert.Equal("Pixel 8", call.Request.DeviceName);
            Assert.Equal("1.2.3", call.Request.AppVersion);
        }

        [Fact]
        public async Task RegisterCurrentAsync_skips_backend_when_platform_token_unavailable()
        {
            var platformTokenProvider = new FakePlatformTokenProvider(
                CottonRemotePushPlatformTokenSnapshot.Unavailable(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Google Play services unavailable."));
            var deviceTokenService = new FakeDeviceTokenService(CreateDeviceTokenSnapshot());
            var coordinator = new CottonRemotePushRegistrationCoordinator(
                platformTokenProvider,
                deviceTokenService);

            CottonRemotePushRegistrationResult result = await coordinator.RegisterCurrentAsync(
                InstanceUri,
                deviceName: null,
                appVersion: null);

            Assert.Equal(CottonRemotePushRegistrationStatus.Unavailable, result.Status);
            Assert.Null(result.DeviceToken);
            Assert.Empty(deviceTokenService.RegistrationCalls);
            Assert.Equal("Google Play services unavailable.", result.PlatformToken.StatusReason);
        }

        [Fact]
        public async Task RegisterCurrentAsync_marks_not_configured_without_backend_call()
        {
            var platformTokenProvider = new FakePlatformTokenProvider(
                CottonRemotePushPlatformTokenSnapshot.NotConfigured(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Firebase app is not configured."));
            var deviceTokenService = new FakeDeviceTokenService(CreateDeviceTokenSnapshot());
            var coordinator = new CottonRemotePushRegistrationCoordinator(
                platformTokenProvider,
                deviceTokenService);

            CottonRemotePushRegistrationResult result = await coordinator.RegisterCurrentAsync(
                InstanceUri,
                deviceName: null,
                appVersion: null);

            Assert.Equal(CottonRemotePushRegistrationStatus.NotConfigured, result.Status);
            Assert.Null(result.DeviceToken);
            Assert.Empty(deviceTokenService.RegistrationCalls);
        }

        [Fact]
        public void PlatformTokenSnapshot_requires_token_when_available()
        {
            Assert.Throws<ArgumentException>(
                () => CottonRemotePushPlatformTokenSnapshot.Available(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    " "));

            CottonRemotePushPlatformTokenSnapshot unavailable =
                CottonRemotePushPlatformTokenSnapshot.Unavailable(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    " unavailable ");

            Assert.False(unavailable.HasToken);
            Assert.Equal("unavailable", unavailable.StatusReason);
        }

        private static CottonRemotePushDeviceTokenSnapshot CreateDeviceTokenSnapshot()
        {
            return new CottonRemotePushDeviceTokenSnapshot(
                TokenId,
                CottonRemotePushProviderKind.FirebaseCloudMessaging,
                CottonRemotePushMobilePlatform.Android,
                "session-1",
                "Pixel 8",
                "1.2.3",
                RegisteredAt,
                revokedAt: null);
        }

        private class FakePlatformTokenProvider : ICottonRemotePushPlatformTokenProvider
        {
            public FakePlatformTokenProvider(CottonRemotePushPlatformTokenSnapshot token)
            {
                Token = token;
            }

            public CottonRemotePushPlatformTokenSnapshot Token { get; }

            public Task<CottonRemotePushPlatformTokenSnapshot> GetCurrentTokenAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Token);
            }
        }

        private class FakeDeviceTokenService : ICottonRemotePushDeviceTokenService
        {
            public FakeDeviceTokenService(CottonRemotePushDeviceTokenSnapshot response)
            {
                Response = response;
            }

            public CottonRemotePushDeviceTokenSnapshot Response { get; }

            public List<RegistrationCall> RegistrationCalls { get; } = [];

            public Task<CottonRemotePushDeviceTokenSnapshot> RegisterCurrentAsync(
                Uri instanceUri,
                CottonRemotePushDeviceTokenRegistrationRequest request,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegistrationCalls.Add(new RegistrationCall(instanceUri, request));
                return Task.FromResult(Response);
            }

            public Task<CottonRemotePushDeviceTokenRevocationResult> RevokeCurrentSessionAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(new CottonRemotePushDeviceTokenRevocationResult(0));
            }
        }

        private class RegistrationCall
        {
            public RegistrationCall(
                Uri instanceUri,
                CottonRemotePushDeviceTokenRegistrationRequest request)
            {
                InstanceUri = instanceUri;
                Request = request;
            }

            public Uri InstanceUri { get; }

            public CottonRemotePushDeviceTokenRegistrationRequest Request { get; }
        }
    }
}
