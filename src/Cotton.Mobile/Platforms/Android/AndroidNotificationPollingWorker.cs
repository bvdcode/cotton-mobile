// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidNotificationPollingWorker")]
    public class AndroidNotificationPollingWorker(
        Context context,
        WorkerParameters workerParameters) : Worker(context, workerParameters)
    {
        public override ListenableWorker.Result DoWork()
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            ICottonNotificationPollingService? pollingService = services?
                .GetService<ICottonNotificationPollingService>();
            if (pollingService is null)
            {
                return Retry();
            }

            try
            {
                pollingService.CheckAsync().GetAwaiter().GetResult();
                return Success();
            }
            catch (Exception exception)
            {
                if (services?.GetService<ILogger<AndroidNotificationPollingWorker>>() is ILogger logger)
                {
                    CottonLog.Warning(logger, "Android background notification polling failed.", exception);
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
