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
            IReadOnlyList<CottonNotificationDto> newestPage,
            int totalCount,
            CottonNotificationCursor? cursor)
        {
            ArgumentNullException.ThrowIfNull(newestPage);
            ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

            if (newestPage.Count > totalCount)
            {
                throw new ArgumentException(
                    "The notification page cannot contain more items than the server total.",
                    nameof(newestPage));
            }

            Guid? newestNotificationId = newestPage.Count == 0
                ? null
                : newestPage[0].Id;
            CottonNotificationCursor nextCursor = new(newestNotificationId, totalCount);
            int unseenCount = ResolveUnseenCount(newestPage, totalCount, cursor);
            if (unseenCount == 0 || newestPage.Count == 0)
            {
                return new CottonNotificationDeliveryPlan([], 0, nextCursor);
            }

            int availableDetails = Math.Min(unseenCount, newestPage.Count);
            if (unseenCount <= _maximumIndividualNotifications
                && availableDetails == unseenCount)
            {
                CottonNotificationDto[] notifications = [.. newestPage.Take(unseenCount)];
                return new CottonNotificationDeliveryPlan(notifications, unseenCount, nextCursor);
            }

            return new CottonNotificationDeliveryPlan([newestPage[0]], unseenCount, nextCursor);
        }

        private static int ResolveUnseenCount(
            IReadOnlyList<CottonNotificationDto> newestPage,
            int totalCount,
            CottonNotificationCursor? cursor)
        {
            if (cursor is null)
            {
                return totalCount;
            }

            if (cursor.LastNotificationId.HasValue)
            {
                int cursorIndex = FindNotificationIndex(newestPage, cursor.LastNotificationId.Value);
                if (cursorIndex >= 0)
                {
                    return cursorIndex;
                }
            }

            return Math.Max(0, totalCount - cursor.TotalCount);
        }

        private static int FindNotificationIndex(
            IReadOnlyList<CottonNotificationDto> notifications,
            Guid notificationId)
        {
            for (int index = 0; index < notifications.Count; index++)
            {
                if (notifications[index].Id == notificationId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
