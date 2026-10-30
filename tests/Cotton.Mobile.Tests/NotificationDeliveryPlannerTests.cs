// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationDeliveryPlannerTests
    {
        private static readonly DateTime CursorCreatedAt =
            new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        private readonly CottonNotificationDeliveryPlanner _planner = new();

        [Fact]
        public void CreateUsesSummaryForLargeUnreadBacklog()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationCursor nextCursor = CreateCursor();
            CottonNotificationBatch batch = new([newest], 1825, nextCursor);

            CottonNotificationDeliveryPlan plan = _planner.Create(batch);

            Assert.True(plan.IsSummary);
            Assert.Equal(1825, plan.UnreadCount);
            Assert.Same(newest, Assert.Single(plan.Notifications));
            Assert.Same(nextCursor, plan.NextCursor);
        }

        [Fact]
        public void CreateReturnsEachUnreadNotificationWithinIndividualLimit()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationDto second = CreateNotification();
            CottonNotificationBatch batch = new([newest, second], 2, CreateCursor());

            CottonNotificationDeliveryPlan plan = _planner.Create(batch);

            Assert.False(plan.IsSummary);
            Assert.Equal(2, plan.UnreadCount);
            Assert.Equal([newest, second], plan.Notifications);
        }

        [Fact]
        public void CreateUsesSummaryWhenServerCapsUnreadDetails()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationDto second = CreateNotification();
            CottonNotificationBatch batch = new([newest, second], 3, CreateCursor());

            CottonNotificationDeliveryPlan plan = _planner.Create(batch);

            Assert.True(plan.IsSummary);
            Assert.Equal(3, plan.UnreadCount);
            Assert.Same(newest, Assert.Single(plan.Notifications));
        }

        [Fact]
        public void CreateAdvancesHighWatermarkWithoutUnreadNotifications()
        {
            CottonNotificationCursor nextCursor = CreateCursor();
            CottonNotificationBatch batch = new([], 0, nextCursor);

            CottonNotificationDeliveryPlan plan = _planner.Create(batch);

            Assert.Equal(0, plan.UnreadCount);
            Assert.Empty(plan.Notifications);
            Assert.Same(nextCursor, plan.NextCursor);
        }

        [Fact]
        public void BatchRejectsUnreadCountWithoutNotificationDetails()
        {
            Assert.Throws<ArgumentException>(() =>
                new CottonNotificationBatch([], 1, CreateCursor()));
        }

        private static CottonNotificationCursor CreateCursor()
        {
            return new CottonNotificationCursor(CursorCreatedAt, Guid.NewGuid());
        }

        private static CottonNotificationDto CreateNotification()
        {
            return new CottonNotificationDto
            {
                Id = Guid.NewGuid(),
                Title = "Notification",
            };
        }
    }
}
