// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationDeliveryPlanner
    {
        private const int DefaultMaximumIndividualNotifications = 3;

        private readonly int _maximumIndividualNotifications;

        public CottonNotificationDeliveryPlanner()
            : this(DefaultMaximumIndividualNotifications)
        {
        }

        public CottonNotificationDeliveryPlanner(int maximumIndividualNotifications)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumIndividualNotifications);

            _maximumIndividualNotifications = maximumIndividualNotifications;
        }

        public CottonNotificationDeliveryPlan Create(
            CottonNotificationBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);

            if (batch.UnreadCount == 0)
            {
                return new CottonNotificationDeliveryPlan([], 0, batch.NextCursor);
            }

            if (batch.UnreadCount <= _maximumIndividualNotifications
                && batch.UnreadNotifications.Count == batch.UnreadCount)
            {
                return new CottonNotificationDeliveryPlan(
                    batch.UnreadNotifications,
                    batch.UnreadCount,
                    batch.NextCursor);
            }

            return new CottonNotificationDeliveryPlan(
                [batch.UnreadNotifications[0]],
                batch.UnreadCount,
                batch.NextCursor);
        }
    }
}
