using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidRemotePushTokenRefreshCoordinatorTests
    {
        [Fact]
        public async Task Schedules_monthly_network_constrained_refresh()
        {
            var host = new FakeRemotePushTokenRefreshHost();
            var coordinator = new CottonAndroidRemotePushTokenRefreshCoordinator(host);

            CottonAndroidRemotePushTokenRefreshScheduleResult result =
                await coordinator.ScheduleAsync();

            Assert.True(result.IsScheduled);
            CottonAndroidRemotePushTokenRefreshRequest request = Assert.Single(host.Requests);
            Assert.Equal(TimeSpan.FromDays(30), request.RefreshInterval);
            Assert.True(request.RequiresNetwork);
            Assert.Equal(
                CottonAndroidRemotePushTokenRefreshScheduleIdentity.WorkName,
                request.ScheduleIdentity.UniqueWorkName);
            Assert.Equal(
                request.ScheduleIdentity.UniqueWorkName,
                request.ScheduleIdentity.RefreshTag);
            Assert.Same(request, result.Request);
        }

        [Fact]
        public async Task Disabled_host_returns_unsupported_without_marking_request_scheduled()
        {
            var coordinator = new CottonAndroidRemotePushTokenRefreshCoordinator(
                DisabledCottonAndroidRemotePushTokenRefreshHost.Instance);

            CottonAndroidRemotePushTokenRefreshScheduleResult result =
                await coordinator.ScheduleAsync();

            Assert.Equal(CottonAndroidRemotePushTokenRefreshScheduleStatus.Unsupported, result.Status);
            Assert.False(result.IsScheduled);
            Assert.Equal("Android remote push token refresh is unavailable on this platform.", result.StatusText);
            Assert.NotNull(result.Request);
        }

        [Fact]
        public async Task Cancels_stable_periodic_refresh_identity()
        {
            var host = new FakeRemotePushTokenRefreshHost();
            var coordinator = new CottonAndroidRemotePushTokenRefreshCoordinator(host);

            CottonAndroidRemotePushTokenRefreshCancelResult result =
                await coordinator.CancelAsync();

            Assert.True(result.IsCancelled);
            CottonAndroidRemotePushTokenRefreshScheduleIdentity identity = Assert.Single(host.Cancellations);
            Assert.Equal(CottonAndroidRemotePushTokenRefreshScheduleIdentity.WorkName, identity.UniqueWorkName);
            Assert.Equal(identity.UniqueWorkName, identity.RefreshTag);
        }

        [Fact]
        public async Task Disabled_host_returns_unsupported_cancel_result()
        {
            var coordinator = new CottonAndroidRemotePushTokenRefreshCoordinator(
                DisabledCottonAndroidRemotePushTokenRefreshHost.Instance);

            CottonAndroidRemotePushTokenRefreshCancelResult result =
                await coordinator.CancelAsync();

            Assert.False(result.IsCancelled);
            Assert.Equal(
                "Android remote push token refresh cancellation is unavailable on this platform.",
                result.StatusText);
        }

        [Fact]
        public void Request_rejects_interval_below_workmanager_periodic_minimum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonAndroidRemotePushTokenRefreshRequest(TimeSpan.FromMinutes(14)));

            var request = new CottonAndroidRemotePushTokenRefreshRequest(TimeSpan.FromMinutes(15));

            Assert.Equal(TimeSpan.FromMinutes(15), request.RefreshInterval);
        }

        private class FakeRemotePushTokenRefreshHost : ICottonAndroidRemotePushTokenRefreshHost
        {
            public List<CottonAndroidRemotePushTokenRefreshRequest> Requests { get; } = [];

            public List<CottonAndroidRemotePushTokenRefreshScheduleIdentity> Cancellations { get; } = [];

            public Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
                CottonAndroidRemotePushTokenRefreshRequest request,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                return Task.FromResult(
                    CottonAndroidRemotePushTokenRefreshScheduleResult.Scheduled(
                        request,
                        "Scheduled."));
            }

            public Task<CottonAndroidRemotePushTokenRefreshCancelResult> CancelAsync(
                CottonAndroidRemotePushTokenRefreshScheduleIdentity scheduleIdentity,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Cancellations.Add(scheduleIdentity);
                return Task.FromResult(CottonAndroidRemotePushTokenRefreshCancelResult.Cancelled());
            }
        }
    }
}
