// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationCursor
    {
        public CottonNotificationCursor(DateTime createdAt, Guid notificationId)
        {
            if (createdAt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Notification cursor timestamp must be UTC.", nameof(createdAt));
            }

            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException("Notification cursor id is required.", nameof(notificationId));
            }

            CreatedAt = createdAt;
            NotificationId = notificationId;
        }

        public DateTime CreatedAt { get; }

        public Guid NotificationId { get; }
    }
}
