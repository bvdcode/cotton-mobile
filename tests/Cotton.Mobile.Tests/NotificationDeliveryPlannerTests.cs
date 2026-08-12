// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationDeliveryPlannerTests
    {
        private readonly CottonNotificationDeliveryPlanner _planner = new();

        [Fact]
        public void CreateUsesSummaryForLargeInitialBacklog()
        {
            CottonNotificationDto newest = CreateNotification();

            CottonNotificationDeliveryPlan plan = _planner.Create([newest], 1825, cursor: null);

            Assert.True(plan.IsSummary);
            Assert.Equal(1825, plan.UnseenCount);
            Assert.Same(newest, Assert.Single(plan.Notifications));
            Assert.Equal(newest.Id, plan.NextCursor.LastNotificationId);
            Assert.Equal(1825, plan.NextCursor.TotalCount);
        }

        [Fact]
        public void CreateReturnsEachNewNotificationWithinIndividualLimit()
        {
            CottonNotificationDto secondNew = CreateNotification();
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationDto previous = CreateNotification();
            CottonNotificationCursor cursor = new(previous.Id, 10);

            CottonNotificationDeliveryPlan plan = _planner.Create(
                [newest, secondNew, previous],
                12,
                cursor);

            Assert.False(plan.IsSummary);
            Assert.Equal(2, plan.UnseenCount);
            Assert.Equal([newest, secondNew], plan.Notifications);
        }

        [Fact]
        public void CreateUsesServerCountWhenCursorIsOutsideNewestPage()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationCursor cursor = new(Guid.NewGuid(), 1);

            CottonNotificationDeliveryPlan plan = _planner.Create([newest], 1826, cursor);

            Assert.True(plan.IsSummary);
            Assert.Equal(1825, plan.UnseenCount);
            Assert.Same(newest, Assert.Single(plan.Notifications));
        }

        [Fact]
        public void CreateDoesNotRepeatCurrentCursor()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationCursor cursor = new(newest.Id, 8);

            CottonNotificationDeliveryPlan plan = _planner.Create([newest], 8, cursor);

            Assert.Equal(0, plan.UnseenCount);
            Assert.Empty(plan.Notifications);
        }

        [Fact]
        public void CreateRebaselinesWithoutRepeatingWhenServerTotalDecreases()
        {
            CottonNotificationDto newest = CreateNotification();
            CottonNotificationCursor cursor = new(Guid.NewGuid(), 8);

            CottonNotificationDeliveryPlan plan = _planner.Create([newest], 7, cursor);

            Assert.Equal(0, plan.UnseenCount);
            Assert.Empty(plan.Notifications);
            Assert.Equal(newest.Id, plan.NextCursor.LastNotificationId);
            Assert.Equal(7, plan.NextCursor.TotalCount);
        }

        [Fact]
        public void CreateTracksAnEmptyNotificationCollection()
        {
            CottonNotificationDeliveryPlan plan = _planner.Create([], 0, cursor: null);

            Assert.Equal(0, plan.UnseenCount);
            Assert.Empty(plan.Notifications);
            Assert.Null(plan.NextCursor.LastNotificationId);
            Assert.Equal(0, plan.NextCursor.TotalCount);
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
