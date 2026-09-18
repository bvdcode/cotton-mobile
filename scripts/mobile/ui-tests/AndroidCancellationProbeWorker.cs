// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidCancellationProbeWorker")]
    public class AndroidCancellationProbeWorker(Context context, WorkerParameters parameters)
        : AndroidAsyncWorker(context, parameters)
    {
        public const string WorkName = "cotton.tests.cancellation";

        public override void OnStopped()
        {
            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag, $"system-stopped:reason={StopReason}");
            Stopwatch stopwatch = Stopwatch.StartNew();
            base.OnStopped();
            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag,
                $"stop-callback-returned:milliseconds={stopwatch.ElapsedMilliseconds}");
        }

        protected override async Task<ListenableWorker.Result> ExecuteAsync(CancellationToken cancellationToken)
        {
            ILoggerFactory factory = IPlatformApplication.Current!.Services.GetRequiredService<ILoggerFactory>();
            factory.AddProvider(new AndroidSlowStopLogger());
            CottonAutomaticSyncDispatcher dispatcher = new(new AndroidCancellationProbeRunner());
            await dispatcher.RunAsync(
                new CottonAuthenticatedSessionScope(new Uri("https://cancellation-test.invalid"), "cancellation-test"),
                CottonAutomaticSyncTrigger.PeriodicReconciliation,
                cancellationToken).ConfigureAwait(false);
            return Success();
        }
    }
}
#endif
