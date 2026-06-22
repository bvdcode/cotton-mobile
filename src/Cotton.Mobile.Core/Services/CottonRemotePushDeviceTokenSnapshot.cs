// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDeviceTokenSnapshot
    {
        public CottonRemotePushDeviceTokenSnapshot(
            Guid id,
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            string sessionId,
            string? deviceName,
            string? appVersion,
            DateTime lastRegisteredAt,
            DateTime? revokedAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Push token id is required.", nameof(id));
            }

            if (!Enum.IsDefined(provider))
            {
                throw new ArgumentOutOfRangeException(nameof(provider));
            }

            if (!Enum.IsDefined(platform))
            {
                throw new ArgumentOutOfRangeException(nameof(platform));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

            Id = id;
            Provider = provider;
            Platform = platform;
            SessionId = sessionId.Trim();
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
            AppVersion = string.IsNullOrWhiteSpace(appVersion) ? null : appVersion.Trim();
            LastRegisteredAt = lastRegisteredAt;
            RevokedAt = revokedAt;
        }

        public Guid Id { get; }

        public CottonRemotePushProviderKind Provider { get; }

        public CottonRemotePushMobilePlatform Platform { get; }

        public string SessionId { get; }

        public string? DeviceName { get; }

        public string? AppVersion { get; }

        public DateTime LastRegisteredAt { get; }

        public DateTime? RevokedAt { get; }
    }
}
