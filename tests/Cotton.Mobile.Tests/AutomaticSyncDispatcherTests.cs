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
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.ApplicationResumed);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
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
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
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
        public async Task CallerCancellationStopsSharedRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            using CancellationTokenSource cancellationSource = new();
            Task<CottonAutomaticSyncRunResult> run = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.PeriodicReconciliation,
                cancellationSource.Token);
            await runner.WaitForNextRunAsync();

            await cancellationSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        }

        [Fact]
        public async Task SelectedRootsCollapseIntoOneFollowUpRun()
        {
            using ControlledAutomaticSyncRunner runner = new();
            CottonAutomaticSyncDispatcher dispatcher = new(runner);
            Guid firstRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid secondRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Task<CottonAutomaticSyncRunResult> first = dispatcher.RunRootsAsync(
                SyncTestRootFactory.InstanceUri,
                [firstRootId]);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(
                SyncTestRootFactory.InstanceUri,
                [firstRootId]);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunRootsAsync(
                SyncTestRootFactory.InstanceUri,
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
                SyncTestRootFactory.InstanceUri,
                CottonAutomaticSyncTrigger.MediaStoreChanged);
            await runner.WaitForNextRunAsync();

            Task<CottonAutomaticSyncRunResult> second = dispatcher.RunRootsAsync(
                SyncTestRootFactory.InstanceUri,
                [rootId]);
            Task<CottonAutomaticSyncRunResult> third = dispatcher.RunAsync(
                SyncTestRootFactory.InstanceUri,
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
