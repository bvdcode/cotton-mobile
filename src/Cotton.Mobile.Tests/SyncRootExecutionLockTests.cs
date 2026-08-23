using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootExecutionLockTests
    {
        [Fact]
        public async Task OperationsForTheSameRootDoNotOverlap()
        {
            CottonSyncRootExecutionLock executionLock = new();
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Task<int> first = executionLock.ExecuteAsync(
                root,
                async cancellationToken =>
                {
                    firstStarted.SetResult();
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                    return 1;
                });
            await firstStarted.Task;
            Task<int> second = executionLock.ExecuteAsync(
                root,
                cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    secondStarted.SetResult();
                    return Task.FromResult(2);
                });

            Assert.False(secondStarted.Task.IsCompleted);
            releaseFirst.SetResult();

            Assert.Equal(1, await first);
            Assert.Equal(2, await second);
            Assert.True(secondStarted.Task.IsCompletedSuccessfully);
            Assert.Equal(0, executionLock.ActiveEntryCount);
        }

        [Fact]
        public async Task CompletedRootChurnDoesNotRetainLockEntries()
        {
            CottonSyncRootExecutionLock executionLock = new();

            for (int index = 0; index < 1_000; index++)
            {
                CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot(
                    rootKey: $"content://tree/root-{index}");
                int result = await executionLock.ExecuteAsync(root, _ => Task.FromResult(index));
                Assert.Equal(index, result);
            }

            Assert.Equal(0, executionLock.ActiveEntryCount);
        }

        [Fact]
        public async Task CanceledWaiterDoesNotReleaseOrRetainTheActiveEntry()
        {
            CottonSyncRootExecutionLock executionLock = new();
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<int> active = executionLock.ExecuteAsync(
                root,
                async cancellationToken =>
                {
                    started.SetResult();
                    await release.Task.WaitAsync(cancellationToken);
                    return 1;
                });
            await started.Task;
            using CancellationTokenSource cancellation = new();
            Task<int> waiting = executionLock.ExecuteAsync(
                root,
                _ => Task.FromResult(2),
                cancellation.Token);

            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            Assert.Equal(1, executionLock.ActiveEntryCount);
            release.SetResult();

            Assert.Equal(1, await active);
            Assert.Equal(0, executionLock.ActiveEntryCount);
        }
    }
}
