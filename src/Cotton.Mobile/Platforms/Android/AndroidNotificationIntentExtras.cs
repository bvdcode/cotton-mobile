// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile
{
    internal static class AndroidNotificationIntentExtras
    {
        public const string IsNotificationLaunch = "dev.cottoncloud.app.extra.NOTIFICATION_LAUNCH";

        public const string NotificationId = "dev.cottoncloud.app.extra.NOTIFICATION_ID";

        public const string EventCategory = "dev.cottoncloud.app.extra.NOTIFICATION_EVENT_CATEGORY";
    }
}
#endif
