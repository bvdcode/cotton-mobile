// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDeviceTokenRegistrationRequest
    {
        public const int TokenMaxLength = 4096;
        public const int DeviceNameMaxLength = 128;
        public const int AppVersionMaxLength = 64;

        public CottonRemotePushDeviceTokenRegistrationRequest(
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string token,
            string? deviceName,
            string? appVersion)
        {
            if (!Enum.IsDefined(provider))
            {
                throw new ArgumentOutOfRangeException(nameof(provider));
            }

            if (!Enum.IsDefined(platform))
            {
                throw new ArgumentOutOfRangeException(nameof(platform));
            }

            string normalizedToken = token?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                throw new ArgumentException("Push token is required.", nameof(token));
            }

            if (normalizedToken.Length > TokenMaxLength)
            {
                throw new ArgumentException("Push token is too long.", nameof(token));
            }

            Provider = provider;
            Platform = platform;
            Token = normalizedToken;
            DeviceName = NormalizeOptional(deviceName, DeviceNameMaxLength);
            AppVersion = NormalizeOptional(appVersion, AppVersionMaxLength);
        }

        public CottonRemotePushProviderKind Provider { get; }

        public CottonRemotePushMobilePlatform Platform { get; }

        public string Token { get; }

        public string? DeviceName { get; }

        public string? AppVersion { get; }

        public static CottonRemotePushDeviceTokenRegistrationRequest ForAndroidFirebase(
            string token,
            string? deviceName = null,
            string? appVersion = null)
        {
            return new CottonRemotePushDeviceTokenRegistrationRequest(
                CottonRemotePushProviderKind.FirebaseCloudMessaging,
                CottonRemotePushMobilePlatform.Android,
                token,
                deviceName,
                appVersion);
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (normalized is null)
            {
                return null;
            }

            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength];
        }
    }
}
