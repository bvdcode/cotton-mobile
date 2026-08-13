// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    [Register("dev.cottoncloud.mobile.AndroidSyncRootWorker")]
    public class AndroidSyncRootWorker(
        Context context,
        WorkerParameters workerParameters) :
        AndroidAutomaticSyncWorker(context, workerParameters)
    {
        protected override CottonAutomaticSyncTrigger Trigger =>
            CottonAutomaticSyncTrigger.PeriodicReconciliation;

        protected override Guid? RetryRootId
        {
            get
            {
                string? rootId = InputData.GetString(AndroidAutomaticSyncConstants.RootIdInputKey);
                if (!Guid.TryParse(rootId, out Guid parsedRootId) || parsedRootId == Guid.Empty)
                {
                    throw new InvalidDataException("Android sync-root work has an invalid root id.");
                }

                return parsedRootId;
            }
        }
    }
}
#endif
