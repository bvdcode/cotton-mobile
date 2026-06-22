// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshRequest
    {
        public static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromDays(30);

        public CottonAndroidRemotePushTokenRefreshRequest(TimeSpan refreshInterval)
        {
            if (refreshInterval < MinimumRefreshInterval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(refreshInterval),
                    "Android periodic token refresh must not be scheduled more often than WorkManager allows.");
            }

            RefreshInterval = refreshInterval;
            RequiresNetwork = true;
            ScheduleIdentity = new CottonAndroidRemotePushTokenRefreshScheduleIdentity();
        }

        public TimeSpan RefreshInterval { get; }

        public bool RequiresNetwork { get; }

        public CottonAndroidRemotePushTokenRefreshScheduleIdentity ScheduleIdentity { get; }

        public static CottonAndroidRemotePushTokenRefreshRequest CreateDefault()
        {
            return new CottonAndroidRemotePushTokenRefreshRequest(DefaultRefreshInterval);
        }
    }
}
