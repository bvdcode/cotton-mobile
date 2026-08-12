// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationPollingServiceTests
    {
        private const int ExpectedPageSize = 50;

        [Fact]
        public async Task CheckAsyncPreservesCursorWhenNotificationPermissionIsDenied()
        {
            CottonNotificationDto notification = CreateNotification();
            CottonNotificationCursor originalCursor = new(Guid.NewGuid(), 1);
            StubCottonNotificationPageProvider pageProvider = new(
                new CottonNotificationPage([notification], 2));
            InMemoryCottonNotificationCursorStore cursorStore = new(originalCursor);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.PermissionDenied);
            using CottonNotificationPollingService pollingService = CreateService(
                pageProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(ExpectedPageSize, pageProvider.RequestedPageSize);
            Assert.Equal(1, localNotificationService.CallCount);
            Assert.Same(originalCursor, cursorStore.Cursor);
            Assert.Equal(0, cursorStore.SaveCount);
        }

        [Fact]
        public async Task CheckAsyncAdvancesCursorAfterSuccessfulDelivery()
        {
            CottonNotificationDto notification = CreateNotification();
            StubCottonNotificationPageProvider pageProvider = new(
                new CottonNotificationPage([notification], 1));
            InMemoryCottonNotificationCursorStore cursorStore = new(cursor: null);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                pageProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(1, localNotificationService.CallCount);
            Assert.Equal(1, cursorStore.SaveCount);
            Assert.Equal(notification.Id, cursorStore.Cursor?.LastNotificationId);
        }

        [Fact]
        public async Task CheckAsyncRebaselinesCursorWhenThereIsNothingToDeliver()
        {
            CottonNotificationDto notification = CreateNotification();
            CottonNotificationCursor originalCursor = new(notification.Id, 1);
            StubCottonNotificationPageProvider pageProvider = new(
                new CottonNotificationPage([notification], 1));
            InMemoryCottonNotificationCursorStore cursorStore = new(originalCursor);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                pageProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(0, localNotificationService.CallCount);
            Assert.Equal(1, cursorStore.SaveCount);
            Assert.Equal(notification.Id, cursorStore.Cursor?.LastNotificationId);
        }

        [Fact]
        public async Task CheckAsyncDoesNothingWithoutAnAuthenticatedNotificationPage()
        {
            StubCottonNotificationPageProvider pageProvider = new(page: null);
            InMemoryCottonNotificationCursorStore cursorStore = new(cursor: null);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                pageProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(1, pageProvider.CallCount);
            Assert.Equal(0, localNotificationService.CallCount);
            Assert.Equal(0, cursorStore.SaveCount);
        }

        private static CottonNotificationPollingService CreateService(
            ICottonNotificationPageProvider pageProvider,
            ICottonNotificationCursorStore cursorStore,
            ICottonLocalNotificationService localNotificationService)
        {
            return new CottonNotificationPollingService(
                pageProvider,
                cursorStore,
                new CottonNotificationDeliveryPlanner(),
                localNotificationService);
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
