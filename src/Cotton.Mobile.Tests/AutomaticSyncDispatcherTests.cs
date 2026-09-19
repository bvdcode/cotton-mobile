using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncDispatcherTests
    {
        [Theory]
        [InlineData(
            CottonAutomaticSyncTrigger.ForegroundSessionStarted,
            CottonAutomaticSyncTrigger.PeriodicReconciliation)]
        [InlineData(
            CottonAutomaticSyncTrigger.MediaStoreChanged,
            CottonAutomaticSyncTrigger.PeriodicReconciliation)]
        [InlineData(
            CottonAutomaticSyncTrigger.PeriodicReconciliation,
            CottonAutomaticSyncTrigger.PeriodicReconciliation)]
        [InlineData(
            CottonAutomaticSyncTrigger.ForegroundSessionStarted,
            CottonAutomaticSyncTrigger.ForegroundSessionStarted)]
        [InlineData(
            CottonAutomaticSyncTrigger.MediaStoreChanged,
            CottonAutomaticSyncTrigger.ForegroundSessionStarted)]
        [InlineData(
            CottonAutomaticSyncTrigger.PeriodicReconciliation,
            CottonAutomaticSyncTrigger.ForegroundSessionStarted)]
        public async Task ReconciliationRequestJoinsRunningFullScan(
            CottonAutomaticSyncTrigger firstTrigger,
            CottonAutomaticSyncTrigger joiningTrigger)
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, firstTrigger, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> joining = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, joiningTrigger,
                TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            runner.ReleaseRun();
            await Task.WhenAll(first, joining);

            Assert.Equal([firstTrigger], runner.Triggers);
        }

        [Fact]
        public async Task PeriodicRequestAfterSelectedRootsStillScansAllRoots()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Guid rootId = Guid.NewGuid();
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunRootsAsync(
                SyncTestRootFactory.SessionScope, [rootId], TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> periodic = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation,
                TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            runner.ReleaseRun();
            await Task.WhenAll(first, periodic);

            Assert.Equal([rootId], Assert.Single(runner.RootSelections));
            Assert.Equal([CottonAutomaticSyncTrigger.PeriodicReconciliation], runner.Triggers);
        }

        [Fact]
        public async Task ConcurrentTriggersCollapseIntoOneFollowUpRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.ForegroundSessionStarted, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            await runner.WaitForNextRunAsync();

            Assert.Equal(2, runner.Triggers.Count);
            runner.ReleaseRun();
            await Task.WhenAll(first, second, third);
            Assert.Equal(2, runner.Triggers.Count);
        }

        [Fact]
        public async Task BroadTriggerSupersedesPendingMediaTrigger()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await Task.WhenAll(first, second, third);

            Assert.Equal(
                [
                    CottonAutomaticSyncTrigger.MediaStoreChanged,
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                ],
                runner.Triggers);
        }

        [Fact]
        public async Task CallerCancellationDoesNotStopSharedRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CancellationTokenSource cancellationSource = new();
            Task<CottonAutomaticSyncRunResult> cancelledWait = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.PeriodicReconciliation,
                cancellationSource.Token);
            await runner.WaitForNextRunAsync();
            Task<CottonAutomaticSyncRunResult> survivingWait = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);

            await cancellationSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWait);
            Assert.False(survivingWait.IsCompleted);
            runner.ReleaseRun();
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await survivingWait;
            Assert.Equal(
                [
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                    CottonAutomaticSyncTrigger.MediaStoreChanged,
                ],
                runner.Triggers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LastCallerCancellationStopsUnderlyingRunAndAllowsLaterWork(bool selectedRoots)
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CancellationTokenSource cancellation = new();
            Task<CottonAutomaticSyncRunResult> waiting = selectedRoots
                ? dispatcher.RunRootsAsync(SyncTestRootFactory.SessionScope, [Guid.NewGuid()], cancellation.Token)
                : dispatcher.RunAsync(SyncTestRootFactory.SessionScope,
                    CottonAutomaticSyncTrigger.PeriodicReconciliation, cancellation.Token);
            await runner.WaitForNextRunAsync();
            CancellationToken executionToken = Assert.Single(runner.ExecutionTokens);

            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            try
            {
                Assert.True(executionToken.IsCancellationRequested, "The upload kept running after its last caller stopped.");
            }
            finally
            {
                dispatcher.Cancel(SyncTestRootFactory.SessionScope);
            }

            Task<CottonAutomaticSyncRunResult> next = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation,
                TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await next.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task UnderlyingRunStopsWhenBothCallersCancel()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CancellationTokenSource firstCancellation = new();
            using CancellationTokenSource secondCancellation = new();
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation,
                firstCancellation.Token);
            await runner.WaitForNextRunAsync();
            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged,
                secondCancellation.Token);
            CancellationToken executionToken = Assert.Single(runner.ExecutionTokens);

            await firstCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            Assert.False(executionToken.IsCancellationRequested);
            await secondCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
            try
            {
                Assert.True(executionToken.IsCancellationRequested, "The upload outlived both callers.");
            }
            finally
            {
                dispatcher.Cancel(SyncTestRootFactory.SessionScope);
            }
        }

        [Fact]
        public async Task DifferentAccountsOnTheSameInstanceDoNotShareExecution()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            CottonAuthenticatedSessionScope otherAccountScope = new(
                SyncTestRootFactory.InstanceUri,
                "account-2");
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(otherAccountScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Assert.Equal(2, runner.Triggers.Count);
            runner.ReleaseRun();
            runner.ReleaseRun();
            await Task.WhenAll(first, second);
        }

        [Fact]
        public async Task SelectedRootsCollapseIntoOneFollowUpRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Guid firstRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid secondRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunRootsAsync(SyncTestRootFactory.SessionScope, [firstRootId], TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(SyncTestRootFactory.SessionScope, [firstRootId], TestContext.Current.CancellationToken);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunRootsAsync(SyncTestRootFactory.SessionScope, [secondRootId], TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await Task.WhenAll(first, second, third);

            Assert.Equal(2, runner.RootSelections.Count);
            Assert.Equal([firstRootId], runner.RootSelections[0]);
            Assert.Equal([firstRootId, secondRootId], runner.RootSelections[1]);
        }

        [Fact]
        public async Task BroadTriggerSupersedesPendingSelectedRoots()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Guid rootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.MediaStoreChanged, TestContext.Current.CancellationToken);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(SyncTestRootFactory.SessionScope, [rootId], TestContext.Current.CancellationToken);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(SyncTestRootFactory.SessionScope, CottonAutomaticSyncTrigger.PeriodicReconciliation, TestContext.Current.CancellationToken);
            runner.ReleaseRun();
            await runner.WaitForNextRunAsync();
            runner.ReleaseRun();
            await Task.WhenAll(first, second, third);

            Assert.Empty(runner.RootSelections);
            Assert.Equal(
                [
                    CottonAutomaticSyncTrigger.MediaStoreChanged,
                    CottonAutomaticSyncTrigger.PeriodicReconciliation,
                ],
                runner.Triggers);
        }
    }
}
