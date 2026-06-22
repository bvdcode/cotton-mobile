// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationLaunchState
    {
        event EventHandler? NotificationLaunchRequested;

        int PendingNotificationLaunchCount { get; }

        void NotifyNotificationOpened(CottonNotificationLaunchRequest request);

        CottonNotificationLaunchRequest? TryConsumePendingNotificationLaunch();

        void ClearPendingNotificationLaunches();
    }
}
