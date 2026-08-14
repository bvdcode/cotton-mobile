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

            ICottonSessionService sessionService = services.GetRequiredService<ICottonSessionService>();
            Uri? instanceUri = await sessionService
                .GetRememberedSessionInstanceAsync(cancellationToken)
                .ConfigureAwait(false);
            if (instanceUri is null)
            {
                return Success();
            }

            CottonAutomaticSyncRunResult result = await RunAsync(
                    services,
                    instanceUri,
                    cancellationToken)
                .ConfigureAwait(false);
            ICottonAutomaticSyncBackgroundScheduler scheduler = services
                .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
            if (result.HasFailures)
            {
                if (RetryRootId.HasValue)
                {
                    return Retry();
                }

                await scheduler
                    .ScheduleRootRetriesAsync(result.FailedRootIds, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (Trigger == CottonAutomaticSyncTrigger.MediaStoreChanged)
            {
                await scheduler
                    .RescheduleMediaStoreTriggerAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return Success();
        }

        private Task<CottonAutomaticSyncRunResult> RunAsync(
            IServiceProvider services,
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            if (RetryRootId.HasValue)
            {
                CottonAutomaticSyncDispatcher retryDispatcher = services
                    .GetRequiredService<CottonAutomaticSyncDispatcher>();
                return retryDispatcher.RunRootsAsync(instanceUri, [RetryRootId.Value], cancellationToken);
            }

            CottonAutomaticSyncDispatcher dispatcher = services
                .GetRequiredService<CottonAutomaticSyncDispatcher>();
            return dispatcher.RunAsync(instanceUri, Trigger, cancellationToken);
        }
    }
}
#endif
