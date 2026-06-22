// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDiagnosticsSnapshot
    {
        public CottonRemotePushDiagnosticsSnapshot(
            CottonRemotePushCapabilitySnapshot capability,
            CottonRemotePushPlatformTokenSnapshot platformToken,
            CottonRemotePushRegistrationStatus? lastRegistrationStatus,
            DateTimeOffset? lastRegistrationAttemptedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(capability);
            ArgumentNullException.ThrowIfNull(platformToken);

            if (lastRegistrationStatus.HasValue && !Enum.IsDefined(lastRegistrationStatus.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(lastRegistrationStatus));
            }

            Capability = capability;
            PlatformToken = platformToken;
            LastRegistrationStatus = lastRegistrationStatus;
            LastRegistrationAttemptedAtUtc = lastRegistrationAttemptedAtUtc;
        }

        public CottonRemotePushCapabilitySnapshot Capability { get; }

        public CottonRemotePushPlatformTokenSnapshot PlatformToken { get; }

        public CottonRemotePushRegistrationStatus? LastRegistrationStatus { get; }

        public DateTimeOffset? LastRegistrationAttemptedAtUtc { get; }
    }
}
