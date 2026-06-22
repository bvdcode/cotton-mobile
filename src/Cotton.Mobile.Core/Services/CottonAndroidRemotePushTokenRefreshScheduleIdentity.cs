// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshScheduleIdentity
    {
        public const string WorkName = "cotton-remote-push-token-refresh";

        public CottonAndroidRemotePushTokenRefreshScheduleIdentity()
        {
            UniqueWorkName = WorkName;
            RefreshTag = WorkName;
        }

        public string UniqueWorkName { get; }

        public string RefreshTag { get; }
    }
}
