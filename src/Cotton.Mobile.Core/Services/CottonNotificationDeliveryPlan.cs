// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationDeliveryPlan
    {
        public CottonNotificationDeliveryPlan(
            IReadOnlyList<CottonNotificationDto> notifications,
            int unseenCount,
            CottonNotificationCursor nextCursor)
        {
            ArgumentNullException.ThrowIfNull(notifications);
            ArgumentNullException.ThrowIfNull(nextCursor);
            ArgumentOutOfRangeException.ThrowIfNegative(unseenCount);

            if (notifications.Count > unseenCount)
            {
                throw new ArgumentException(
                    "Notification details cannot exceed the unseen notification count.",
                    nameof(notifications));
            }

            Notifications = notifications;
            UnseenCount = unseenCount;
            NextCursor = nextCursor;
        }

        public IReadOnlyList<CottonNotificationDto> Notifications { get; }

        public int UnseenCount { get; }

        public CottonNotificationCursor NextCursor { get; }

        public bool IsSummary => UnseenCount > Notifications.Count;
    }
}
