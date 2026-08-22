using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncSessionServiceTests
    {
        [Fact]
        public async Task ForegroundSessionSetupDoesNotWaitForSynchronization()
        {
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingAutomaticSyncBackgroundScheduler scheduler = new();
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CottonAutomaticSyncSessionService sessionService = new(
                foregroundService,
                scheduler,
                dispatcher,
                NullLogger<CottonAutomaticSyncSessionService>.Instance);
            sessionService.Initialize();

            Task setSession = sessionService.SetSessionAsync(SyncTestRootFactory.InstanceUri);

            await setSession.WaitAsync(TimeSpan.FromSeconds(5));
            await runner.WaitForNextRunAsync();
            Assert.Equal(1, scheduler.ScheduleCount);
            Assert.False(setSession.IsFaulted);
            runner.ReleaseRun();
        }

        [Fact]
        public async Task ClearingSessionCancelsActiveSynchronization()
        {
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingAutomaticSyncBackgroundScheduler scheduler = new();
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CottonAutomaticSyncSessionService sessionService = new(
                foregroundService,
                scheduler,
                dispatcher,
                NullLogger<CottonAutomaticSyncSessionService>.Instance);
            sessionService.Initialize();
            await sessionService.SetSessionAsync(SyncTestRootFactory.InstanceUri);
            await runner.WaitForNextRunAsync();

            await sessionService.SetSessionAsync(instanceUri: null);

            Assert.Equal(1, scheduler.CancelCount);
        }

        [Fact]
        public async Task ForegroundFailureSchedulesOnlyFailedRootForRetry()
        {
            Guid failedRootId = Guid.NewGuid();
            TestApplicationForegroundService foregroundService = new();
            foregroundService.NotifyResumed();
            RecordingAutomaticSyncBackgroundScheduler scheduler = new();
            using ControlledAutomaticSyncRunner runner = new()
            {
                Result = new CottonAutomaticSyncRunResult([], [failedRootId]),
            };
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CottonAutomaticSyncSessionService sessionService = new(
                foregroundService,
                scheduler,
                dispatcher,
                NullLogger<CottonAutomaticSyncSessionService>.Instance);
            sessionService.Initialize();

            await sessionService.SetSessionAsync(SyncTestRootFactory.InstanceUri);
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await scheduler.WaitForRootRetryAsync();

            Assert.Equal([failedRootId], scheduler.RootRetryIds);
        }
    }
}
