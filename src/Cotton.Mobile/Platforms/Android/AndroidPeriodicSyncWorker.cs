// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidPeriodicSyncWorker")]
    public class AndroidPeriodicSyncWorker(
        Context context,
        WorkerParameters workerParameters) :
        AndroidAutomaticSyncWorker(context, workerParameters)
    {
        protected override CottonAutomaticSyncTrigger Trigger =>
            CottonAutomaticSyncTrigger.PeriodicReconciliation;
    }
}
#endif
