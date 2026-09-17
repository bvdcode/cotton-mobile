// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidBudgetProbeWorker")]
    public class AndroidBudgetProbeWorker(Context context, WorkerParameters parameters)
        : AndroidCancellationProbeWorker(context, parameters)
    {
        protected override async Task<ListenableWorker.Result> ExecuteAsync(CancellationToken cancellationToken)
        {
            CottonBackgroundExecutionWindow window = new(TimeSpan.FromSeconds(2));
            try
            {
                return await window.RunAsync(base.ExecuteAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (CottonBackgroundWindowExpiredException)
            {
                _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag, "window-ended:retry");
                return Retry();
            }
        }
    }
}
#endif
