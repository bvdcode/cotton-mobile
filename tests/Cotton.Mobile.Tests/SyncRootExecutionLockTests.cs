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
        }
    }
}
