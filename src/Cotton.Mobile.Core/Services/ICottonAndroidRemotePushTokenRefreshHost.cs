// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidRemotePushTokenRefreshHost
    {
        Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CottonAndroidRemotePushTokenRefreshRequest request,
            CancellationToken cancellationToken = default);

        Task<CottonAndroidRemotePushTokenRefreshCancelResult> CancelAsync(
            CottonAndroidRemotePushTokenRefreshScheduleIdentity scheduleIdentity,
            CancellationToken cancellationToken = default);
    }
}
