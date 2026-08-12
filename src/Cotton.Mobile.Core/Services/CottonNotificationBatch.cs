// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationBatch
    {
        public CottonNotificationBatch(
            IReadOnlyList<CottonNotificationDto> unreadNotifications,
            int unreadCount,
            CottonNotificationCursor? nextCursor)
        {
            ArgumentNullException.ThrowIfNull(unreadNotifications);
            ArgumentOutOfRangeException.ThrowIfNegative(unreadCount);

            if (unreadNotifications.Count > unreadCount)
            {
                throw new ArgumentException(
                    "Notification details cannot exceed the unread notification count.",
                    nameof(unreadNotifications));
            }

            if (unreadCount > 0 && unreadNotifications.Count == 0)
            {
                throw new ArgumentException(
                    "A non-empty unread batch requires at least one notification detail.",
                    nameof(unreadNotifications));
            }

            if (unreadCount > 0 && nextCursor is null)
            {
                throw new ArgumentException(
                    "A non-empty unread batch requires a next cursor.",
                    nameof(nextCursor));
            }

            UnreadNotifications = unreadNotifications;
            UnreadCount = unreadCount;
            NextCursor = nextCursor;
        }

        public IReadOnlyList<CottonNotificationDto> UnreadNotifications { get; }

        public int UnreadCount { get; }

        public CottonNotificationCursor? NextCursor { get; }
    }
}
