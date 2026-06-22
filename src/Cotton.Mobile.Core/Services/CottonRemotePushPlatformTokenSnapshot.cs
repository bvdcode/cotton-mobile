// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPlatformTokenSnapshot
    {
        private CottonRemotePushPlatformTokenSnapshot(
            CottonRemotePushPlatformTokenStatus status,
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string? token,
            string? statusReason)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (!Enum.IsDefined(provider))
            {
                throw new ArgumentOutOfRangeException(nameof(provider));
            }

            if (!Enum.IsDefined(platform))
            {
                throw new ArgumentOutOfRangeException(nameof(platform));
            }

            string? normalizedToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
            if (status == CottonRemotePushPlatformTokenStatus.Available
                && string.IsNullOrWhiteSpace(normalizedToken))
            {
                throw new ArgumentException("A push platform token is required when the token is available.", nameof(token));
            }

            Status = status;
            Provider = provider;
            Platform = platform;
            Token = normalizedToken;
            StatusReason = string.IsNullOrWhiteSpace(statusReason) ? null : statusReason.Trim();
        }

        public CottonRemotePushPlatformTokenStatus Status { get; }

        public CottonRemotePushProviderKind Provider { get; }

        public CottonRemotePushMobilePlatform Platform { get; }

        public string? Token { get; }

        public string? StatusReason { get; }

        public bool HasToken => Status == CottonRemotePushPlatformTokenStatus.Available;

        public static CottonRemotePushPlatformTokenSnapshot Available(
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string token)
        {
            return new CottonRemotePushPlatformTokenSnapshot(
                CottonRemotePushPlatformTokenStatus.Available,
                provider,
                platform,
                token,
                statusReason: null);
        }

        public static CottonRemotePushPlatformTokenSnapshot Unavailable(
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string? statusReason = null)
        {
            return new CottonRemotePushPlatformTokenSnapshot(
                CottonRemotePushPlatformTokenStatus.Unavailable,
                provider,
                platform,
                token: null,
                statusReason);
        }

        public static CottonRemotePushPlatformTokenSnapshot NotConfigured(
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string? statusReason = null)
        {
            return new CottonRemotePushPlatformTokenSnapshot(
                CottonRemotePushPlatformTokenStatus.NotConfigured,
                provider,
                platform,
                token: null,
                statusReason);
        }
    }
}
