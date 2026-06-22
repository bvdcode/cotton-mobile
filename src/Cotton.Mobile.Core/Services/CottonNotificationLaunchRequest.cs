// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationLaunchRequest
    {
        public CottonNotificationLaunchRequest(
            Guid notificationId,
            CottonRemotePushEventCategory category)
        {
            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException("Notification id is required.", nameof(notificationId));
            }

            if (!IsSupportedCategory(category))
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Remote push category is not supported.");
            }

            NotificationId = notificationId;
            Category = category;
        }

        public Guid NotificationId { get; }

        public CottonRemotePushEventCategory Category { get; }

        public static CottonNotificationLaunchRequest? TryCreate(
            Guid notificationId,
            CottonRemotePushEventCategory category)
        {
            return notificationId != Guid.Empty && IsSupportedCategory(category)
                ? new CottonNotificationLaunchRequest(notificationId, category)
                : null;
        }

        private static bool IsSupportedCategory(CottonRemotePushEventCategory category)
        {
            return CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend
                .SupportsVisibleEventCategory(category);
        }
    }
}
