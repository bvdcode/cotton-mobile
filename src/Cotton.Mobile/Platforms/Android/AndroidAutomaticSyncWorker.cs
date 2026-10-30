// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    public abstract class AndroidAutomaticSyncWorker(
        Context context,
        WorkerParameters workerParameters) : Worker(context, workerParameters)
    {
        private readonly CancellationTokenSource _stoppingSource = new();

        protected abstract CottonAutomaticSyncTrigger Trigger { get; }

        protected abstract Guid? RetryRootId { get; }

        public override ListenableWorker.Result DoWork()
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            if (services is null)
            {
                return Retry();
            }

            try
            {
                CancellationToken cancellationToken = _stoppingSource.Token;
                ICottonSessionService sessionService = services.GetRequiredService<ICottonSessionService>();
                Uri? instanceUri = sessionService
                    .GetRememberedSessionInstanceAsync(cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                if (instanceUri is null)
                {
                    return Success();
                }

                CottonAutomaticSyncRunResult result = RunAsync(
                        services,
                        instanceUri,
                        cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                ICottonAutomaticSyncBackgroundScheduler scheduler = services
                    .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
                if (result.HasFailures)
                {
                    if (RetryRootId.HasValue)
                    {
                        return Retry();
                    }

                    scheduler
                        .ScheduleRootRetriesAsync(result.FailedRootIds, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }

                if (Trigger == CottonAutomaticSyncTrigger.MediaStoreChanged)
                {
                    scheduler
                        .RescheduleMediaStoreTriggerAsync(cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }

                return Success();
            }
            catch (OperationCanceledException) when (_stoppingSource.IsCancellationRequested)
            {
                return Retry();
            }
            catch (Exception exception)
            {
                ILogger<AndroidAutomaticSyncWorker>? logger = services
                    .GetService<ILogger<AndroidAutomaticSyncWorker>>();
                if (logger is not null)
                {
                    CottonLog.Warning(logger, "Android automatic sync worker failed.", exception);
                }

                return Retry();
            }
        }

        public override void OnStopped()
        {
            _stoppingSource.Cancel();
            base.OnStopped();
        }

        private Task<CottonAutomaticSyncRunResult> RunAsync(
            IServiceProvider services,
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            if (RetryRootId.HasValue)
            {
                ICottonAutomaticSyncRunner runner = services.GetRequiredService<ICottonAutomaticSyncRunner>();
                return runner.RunRootsAsync(instanceUri, [RetryRootId.Value], cancellationToken);
            }

            CottonAutomaticSyncDispatcher dispatcher = services
                .GetRequiredService<CottonAutomaticSyncDispatcher>();
            return dispatcher.RunAsync(instanceUri, Trigger, cancellationToken);
        }

        private static ListenableWorker.Result Success()
        {
            return ListenableWorker.Result.InvokeSuccess()
                ?? throw new InvalidOperationException("Android WorkManager success result is unavailable.");
        }

        private static ListenableWorker.Result Retry()
        {
            return ListenableWorker.Result.InvokeRetry()
                ?? throw new InvalidOperationException("Android WorkManager retry result is unavailable.");
        }
    }
}
#endif
