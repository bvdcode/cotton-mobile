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
        protected abstract CottonAutomaticSyncTrigger Trigger { get; }

        public override ListenableWorker.Result DoWork()
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            if (services is null)
            {
                return Retry();
            }

            try
            {
                ICottonSessionService sessionService = services.GetRequiredService<ICottonSessionService>();
                Uri? instanceUri = sessionService
                    .GetRememberedSessionInstanceAsync()
                    .GetAwaiter()
                    .GetResult();
                if (instanceUri is null)
                {
                    return Success();
                }

                CottonAutomaticSyncRunner runner = services.GetRequiredService<CottonAutomaticSyncRunner>();
                runner.RunAsync(instanceUri, Trigger).GetAwaiter().GetResult();
                if (Trigger == CottonAutomaticSyncTrigger.MediaStoreChanged)
                {
                    ICottonAutomaticSyncBackgroundScheduler scheduler = services
                        .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
                    scheduler.RescheduleMediaStoreTriggerAsync().GetAwaiter().GetResult();
                }

                return Success();
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
