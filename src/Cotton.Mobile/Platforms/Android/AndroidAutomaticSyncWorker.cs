// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    public abstract class AndroidAutomaticSyncWorker(
        Context context,
        WorkerParameters workerParameters) : AndroidAsyncWorker(context, workerParameters)
    {
        protected abstract CottonAutomaticSyncTrigger Trigger { get; }

        protected abstract Guid? RetryRootId { get; }

        protected override async Task<ListenableWorker.Result> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            if (services is null)
            {
                return Retry();
            }

            AndroidAutomaticSyncExecutor executor = services
                .GetRequiredService<AndroidAutomaticSyncExecutor>();
            AndroidAutomaticSyncExecutionResult result = await executor
                .ExecuteAsync(Trigger, RetryRootId, cancellationToken)
                .ConfigureAwait(false);
            return result switch
            {
                AndroidAutomaticSyncExecutionResult.Completed => Success(),
                AndroidAutomaticSyncExecutionResult.NoSession => Success(),
                AndroidAutomaticSyncExecutionResult.RetryRequired => Retry(),
                _ => throw new InvalidOperationException("Sync execution result is not supported."),
            };
        }
    }
}
#endif
