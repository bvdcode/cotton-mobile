// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidNotificationPollingWorker")]
    public class AndroidNotificationPollingWorker(
        Context context,
        WorkerParameters workerParameters) : AndroidAsyncWorker(context, workerParameters)
    {
        protected override async Task<ListenableWorker.Result> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            ICottonNotificationPollingService? pollingService = services?
                .GetService<ICottonNotificationPollingService>();
            if (pollingService is null)
            {
                return Retry();
            }

            await pollingService.CheckAsync(cancellationToken).ConfigureAwait(false);
            return Success();
        }
    }
}
