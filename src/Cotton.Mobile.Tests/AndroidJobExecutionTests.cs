// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Platforms.Android;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidJobExecutionTests
    {
        [Fact]
        public async Task LateStopAfterCompletionDoesNotUseDisposedCancellationSource()
        {
            AndroidJobExecution execution = new();

            await execution.DisposeAsync();
            await execution.CancelAsync();
        }

        [Fact]
        public async Task CompletionWaitsForRunningCancellationCallback()
        {
            AndroidJobExecution execution = new();
            using ManualResetEventSlim release = new(false);
            TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration = execution.Token.Register(() =>
            {
                entered.TrySetResult();
                release.Wait(TestContext.Current.CancellationToken);
            });
            Task stopping = execution.CancelAsync();
            Task? completion = null;
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.Same(stopping, execution.CancelAsync());
                completion = execution.DisposeAsync().AsTask();
                Assert.False(completion.IsCompleted);
            }
            finally
            {
                release.Set();
                await stopping;
                if (completion is not null)
                {
                    await completion;
                }
                else
                {
                    await execution.DisposeAsync();
                }
            }

            await execution.CancelAsync();
        }
    }
}
