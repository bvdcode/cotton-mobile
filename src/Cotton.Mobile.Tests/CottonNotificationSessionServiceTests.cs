// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonNotificationSessionServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example.com");
        private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task SetSessionAsyncStartsForegroundAndBackgroundDelivery()
        {
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingNotificationPermissionService permissionService = new();
            RecordingNotificationBackgroundScheduler backgroundScheduler = new();
            RecordingNotificationRealtimeService realtimeService = new();
            RecordingNotificationPollingService pollingService = new();
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                permissionService,
                backgroundScheduler,
                realtimeService,
                pollingService);

            await service.SetSessionAsync(InstanceUri);

            Assert.Equal(1, backgroundScheduler.ScheduleCount);
            Assert.Equal(1, permissionService.RequestCount);
            Assert.Equal(1, realtimeService.StartCount);
            Assert.Equal(InstanceUri, realtimeService.StartedInstanceUri);
            Assert.Equal(1, pollingService.CheckCount);
        }

        [Fact]
        public async Task SetSessionAsyncInBackgroundOnlySchedulesPolling()
        {
            TestApplicationForegroundService foregroundService = new();
            RecordingNotificationPermissionService permissionService = new();
            RecordingNotificationBackgroundScheduler backgroundScheduler = new();
            RecordingNotificationRealtimeService realtimeService = new();
            RecordingNotificationPollingService pollingService = new();
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                permissionService,
                backgroundScheduler,
                realtimeService,
                pollingService);

            await service.SetSessionAsync(InstanceUri);

            Assert.Equal(1, backgroundScheduler.ScheduleCount);
            Assert.Equal(0, permissionService.RequestCount);
            Assert.Equal(0, realtimeService.StartCount);
            Assert.Equal(0, pollingService.CheckCount);
        }

        [Fact]
        public async Task ResumeStartsRealtimeAndImmediatePolling()
        {
            TestApplicationForegroundService foregroundService = new();
            RecordingNotificationPermissionService permissionService = new();
            RecordingNotificationBackgroundScheduler backgroundScheduler = new();
            RecordingNotificationRealtimeService realtimeService = new();
            RecordingNotificationPollingService pollingService = new();
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                permissionService,
                backgroundScheduler,
                realtimeService,
                pollingService);
            service.Initialize();
            await service.SetSessionAsync(InstanceUri);

            foregroundService.NotifyResumed();
            await Task.WhenAll(realtimeService.FirstStart, pollingService.FirstCheck)
                .WaitAsync(CompletionTimeout);

            Assert.Equal(1, permissionService.RequestCount);
            Assert.Equal(1, realtimeService.StartCount);
            Assert.Equal(1, pollingService.CheckCount);
        }

        [Fact]
        public async Task StopAndResumeAreSerialized()
        {
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingNotificationRealtimeService realtimeService = new()
            {
                BlockStop = true,
            };
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                new RecordingNotificationPermissionService(),
                new RecordingNotificationBackgroundScheduler(),
                realtimeService,
                new RecordingNotificationPollingService());
            service.Initialize();
            await service.SetSessionAsync(InstanceUri);

            foregroundService.NotifyStopped();
            await realtimeService.StopEntered.WaitAsync(CompletionTimeout);
            foregroundService.NotifyResumed();

            Assert.False(realtimeService.SecondStart.IsCompleted);
            realtimeService.ReleaseStop();
            await realtimeService.SecondStart.WaitAsync(CompletionTimeout);
            Assert.Equal(2, realtimeService.StartCount);
        }

        [Fact]
        public async Task ClearingSessionCancelsBackgroundAndStopsRealtime()
        {
            TestApplicationForegroundService foregroundService = new();
            RecordingNotificationBackgroundScheduler backgroundScheduler = new();
            RecordingNotificationRealtimeService realtimeService = new();
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                new RecordingNotificationPermissionService(),
                backgroundScheduler,
                realtimeService,
                new RecordingNotificationPollingService());

            await service.SetSessionAsync(instanceUri: null);

            Assert.Equal(1, backgroundScheduler.CancelCount);
            Assert.Equal(1, realtimeService.StopCount);
        }

        [Fact]
        public async Task DeliveryFailuresDoNotPreventIndependentSteps()
        {
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingNotificationPermissionService permissionService = new(new InvalidOperationException());
            RecordingNotificationBackgroundScheduler backgroundScheduler = new(
                scheduleFailure: new InvalidOperationException());
            RecordingNotificationRealtimeService realtimeService = new(new InvalidOperationException());
            RecordingNotificationPollingService pollingService = new(new InvalidOperationException());
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                permissionService,
                backgroundScheduler,
                realtimeService,
                pollingService);

            await service.SetSessionAsync(InstanceUri);

            Assert.Equal(1, backgroundScheduler.ScheduleCount);
            Assert.Equal(1, permissionService.RequestCount);
            Assert.Equal(1, realtimeService.StartCount);
            Assert.Equal(1, pollingService.CheckCount);
        }

        [Fact]
        public async Task SetSessionAsyncPropagatesCancellationBeforeMutation()
        {
            TestApplicationForegroundService foregroundService = new();
            RecordingNotificationBackgroundScheduler backgroundScheduler = new();
            using CottonNotificationSessionService service = CreateService(
                foregroundService,
                new RecordingNotificationPermissionService(),
                backgroundScheduler,
                new RecordingNotificationRealtimeService(),
                new RecordingNotificationPollingService());
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.SetSessionAsync(InstanceUri, cancellation.Token));

            Assert.Equal(0, backgroundScheduler.ScheduleCount);
        }

        [Fact]
        public async Task DisposeUnsubscribesFromForegroundEvents()
        {
            TestApplicationForegroundService foregroundService = new();
            RecordingNotificationRealtimeService realtimeService = new();
            CottonNotificationSessionService service = CreateService(
                foregroundService,
                new RecordingNotificationPermissionService(),
                new RecordingNotificationBackgroundScheduler(),
                realtimeService,
                new RecordingNotificationPollingService());
            service.Initialize();
            await service.SetSessionAsync(InstanceUri);

            service.Dispose();
            foregroundService.NotifyResumed();

            Assert.Equal(0, realtimeService.StartCount);
        }

        private static CottonNotificationSessionService CreateService(
            IApplicationForegroundService foregroundService,
            ICottonNotificationPermissionService permissionService,
            ICottonNotificationBackgroundScheduler backgroundScheduler,
            ICottonNotificationRealtimeService realtimeService,
            ICottonNotificationPollingService pollingService)
        {
            return new CottonNotificationSessionService(
                foregroundService,
                permissionService,
                backgroundScheduler,
                realtimeService,
                pollingService,
                NullLogger<CottonNotificationSessionService>.Instance);
        }
    }
}
