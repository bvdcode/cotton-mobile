// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BackgroundExecutionWindowTests
    {
        [Fact]
        public async Task DeadlineCancelsOperationAndWaitsForResourceCleanup()
        {
            CottonBackgroundExecutionWindow window = new(TimeSpan.FromMilliseconds(50));
            TaskCompletionSource cancelling = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<int> operation = window.RunAsync(async token =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return 0;
                }
                finally
                {
                    cancelling.TrySetResult();
                    await release.Task;
                }
            }, TestContext.Current.CancellationToken);
            try
            {
                await cancelling.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.False(operation.IsCompleted);
            }
            finally
            {
                release.TrySetResult();
            }

            await Assert.ThrowsAsync<CottonBackgroundWindowExpiredException>(() => operation);
        }

        [Fact]
        public async Task SystemCancellationIsNotReportedAsAnExpiredWindow()
        {
            CottonBackgroundExecutionWindow window = new(TimeSpan.FromSeconds(5));
            using CancellationTokenSource stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            Task<int> operation = window.RunAsync(async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 0;
            }, stopping.Token);

            await stopping.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        }

        [Fact]
        public async Task CompletedOperationReturnsNormallyWithinTheWindow()
        {
            CottonBackgroundExecutionWindow window = new(CottonBackgroundExecutionWindow.DefaultDuration);

            Assert.Equal(42, await window.RunAsync(token => Task.FromResult(42), TestContext.Current.CancellationToken));
            Assert.True(CottonBackgroundExecutionWindow.DefaultDuration < TimeSpan.FromMinutes(10));
        }
    }
}
