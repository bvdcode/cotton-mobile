using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncDispatcherTests
    {
        [Fact]
        public async Task ConcurrentTriggersCollapseIntoOneFollowUpRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.ApplicationResumed);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
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
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.PeriodicReconciliation);
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
            Task<CottonAutomaticSyncRunResult> survivingWait = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);

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

        [Fact]
        public async Task DifferentAccountsOnTheSameInstanceDoNotShareExecution()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            CottonAuthenticatedSessionScope otherAccountScope = new(
                SyncTestRootFactory.InstanceUri,
                "account-2");
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.PeriodicReconciliation);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                otherAccountScope,
                CottonAutomaticSyncTrigger.PeriodicReconciliation);
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
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunRootsAsync(
                SyncTestRootFactory.SessionScope,
                [firstRootId]);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(
                SyncTestRootFactory.SessionScope,
                [firstRootId]);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunRootsAsync(
                SyncTestRootFactory.SessionScope,
                [secondRootId]);
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
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(
                SyncTestRootFactory.SessionScope,
                [rootId]);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.SessionScope,
                CottonAutomaticSyncTrigger.PeriodicReconciliation);
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
