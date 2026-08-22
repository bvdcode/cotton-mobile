// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class NotificationPollingServiceTests
    {
        private const int ExpectedDetailLimit = 50;

        private static readonly DateTime CursorCreatedAt =
            new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task CheckAsyncPreservesCursorWhenNotificationPermissionIsDenied()
        {
            CottonNotificationCursor originalCursor = CreateCursor();
            CottonNotificationCursor nextCursor = CreateCursor(1);
            CottonNotificationDto notification = CreateNotification();
            StubCottonNotificationBatchProvider batchProvider = new(
                new CottonNotificationBatch([notification], 1, nextCursor));
            InMemoryCottonNotificationCursorStore cursorStore = new(originalCursor);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.PermissionDenied);
            using CottonNotificationPollingService pollingService = CreateService(
                batchProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Same(originalCursor, batchProvider.RequestedCursor);
            Assert.Equal(ExpectedDetailLimit, batchProvider.RequestedDetailLimit);
            Assert.Equal(1, localNotificationService.CallCount);
            Assert.Same(originalCursor, cursorStore.Cursor);
            Assert.Equal(0, cursorStore.SaveCount);
        }

        [Fact]
        public async Task CheckAsyncAdvancesCursorAfterSuccessfulDelivery()
        {
            CottonNotificationCursor nextCursor = CreateCursor();
            CottonNotificationDto notification = CreateNotification();
            StubCottonNotificationBatchProvider batchProvider = new(
                new CottonNotificationBatch([notification], 1, nextCursor));
            InMemoryCottonNotificationCursorStore cursorStore = new(cursor: null);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                batchProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(1, localNotificationService.CallCount);
            Assert.Equal(1, cursorStore.SaveCount);
            Assert.Same(nextCursor, cursorStore.Cursor);
        }

        [Fact]
        public async Task CheckAsyncAdvancesCursorForNotificationsReadElsewhere()
        {
            CottonNotificationCursor originalCursor = CreateCursor();
            CottonNotificationCursor nextCursor = CreateCursor(1);
            StubCottonNotificationBatchProvider batchProvider = new(
                new CottonNotificationBatch([], 0, nextCursor));
            InMemoryCottonNotificationCursorStore cursorStore = new(originalCursor);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                batchProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(0, localNotificationService.CallCount);
            Assert.Equal(1, cursorStore.SaveCount);
            Assert.Same(nextCursor, cursorStore.Cursor);
        }

        [Fact]
        public async Task CheckAsyncDoesNotCreateCursorForAnEmptyNotificationStream()
        {
            StubCottonNotificationBatchProvider batchProvider = new(
                new CottonNotificationBatch([], 0, nextCursor: null));
            InMemoryCottonNotificationCursorStore cursorStore = new(cursor: null);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                batchProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(0, localNotificationService.CallCount);
            Assert.Equal(0, cursorStore.SaveCount);
        }

        [Fact]
        public async Task CheckAsyncDoesNothingWithoutAnAuthenticatedNotificationBatch()
        {
            StubCottonNotificationBatchProvider batchProvider = new(batch: null);
            InMemoryCottonNotificationCursorStore cursorStore = new(cursor: null);
            RecordingCottonLocalNotificationService localNotificationService = new(
                CottonLocalNotificationDeliveryStatus.Delivered);
            using CottonNotificationPollingService pollingService = CreateService(
                batchProvider,
                cursorStore,
                localNotificationService);

            await pollingService.CheckAsync();

            Assert.Equal(1, batchProvider.CallCount);
            Assert.Equal(0, localNotificationService.CallCount);
            Assert.Equal(0, cursorStore.SaveCount);
        }

        private static CottonNotificationPollingService CreateService(
            ICottonNotificationBatchProvider batchProvider,
            ICottonNotificationCursorStore cursorStore,
            ICottonLocalNotificationService localNotificationService)
        {
            return new CottonNotificationPollingService(
                batchProvider,
                cursorStore,
                new CottonNotificationDeliveryPlanner(),
                localNotificationService);
        }

        private static CottonNotificationCursor CreateCursor(int seconds = 0)
        {
            return new CottonNotificationCursor(CursorCreatedAt.AddSeconds(seconds), Guid.NewGuid());
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
