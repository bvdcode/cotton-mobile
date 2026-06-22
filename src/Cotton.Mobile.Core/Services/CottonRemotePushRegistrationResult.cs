// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushRegistrationResult
    {
        private CottonRemotePushRegistrationResult(
            CottonRemotePushRegistrationStatus status,
            CottonRemotePushPlatformTokenSnapshot platformToken,
            CottonRemotePushDeviceTokenSnapshot? deviceToken)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            ArgumentNullException.ThrowIfNull(platformToken);

            if (status == CottonRemotePushRegistrationStatus.Registered && deviceToken is null)
            {
                throw new ArgumentException("A registered push token result requires a device token.", nameof(deviceToken));
            }

            Status = status;
            PlatformToken = platformToken;
            DeviceToken = deviceToken;
        }

        public CottonRemotePushRegistrationStatus Status { get; }

        public CottonRemotePushPlatformTokenSnapshot PlatformToken { get; }

        public CottonRemotePushDeviceTokenSnapshot? DeviceToken { get; }

        public static CottonRemotePushRegistrationResult Registered(
            CottonRemotePushPlatformTokenSnapshot platformToken,
            CottonRemotePushDeviceTokenSnapshot deviceToken)
        {
            return new CottonRemotePushRegistrationResult(
                CottonRemotePushRegistrationStatus.Registered,
                platformToken,
                deviceToken);
        }

        public static CottonRemotePushRegistrationResult Skipped(
            CottonRemotePushPlatformTokenSnapshot platformToken)
        {
            CottonRemotePushRegistrationStatus status = platformToken.Status switch
            {
                CottonRemotePushPlatformTokenStatus.NotConfigured =>
                    CottonRemotePushRegistrationStatus.NotConfigured,
                _ => CottonRemotePushRegistrationStatus.Unavailable,
            };

            return new CottonRemotePushRegistrationResult(status, platformToken, deviceToken: null);
        }
    }
}
