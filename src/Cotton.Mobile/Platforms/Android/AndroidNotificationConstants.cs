// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile
{
    public static class AndroidNotificationConstants
    {
        public const string GeneralChannelId = "cotton.notifications";
        public const string SecurityChannelId = "cotton.notifications.security";
        public const string GroupKey = "cotton.notifications.group";
        public const string PeriodicWorkName = "cotton.notifications.poll";
        public const int SummaryNotificationId = 19001;
        public const int PeriodicIntervalMinutes = 15;
    }
}
