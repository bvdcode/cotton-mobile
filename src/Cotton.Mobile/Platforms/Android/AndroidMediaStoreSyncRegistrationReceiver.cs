// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Util;
using AndroidIntentFilter = Android.App.IntentFilterAttribute;

namespace Cotton.Mobile.Platforms.Android
{
    [BroadcastReceiver(Name = ComponentName, Enabled = true, Exported = false)]
    [AndroidIntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
    public class AndroidMediaStoreSyncRegistrationReceiver : BroadcastReceiver
    {
        public const string ComponentName =
            "dev.cottoncloud.mobile.AndroidMediaStoreSyncRegistrationReceiver";

        private const string LogTag = "CottonMediaSyncRegistration";

        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                AndroidMediaStoreSyncJobScheduler.Schedule();
            }
            catch (Exception exception)
            {
                _ = Log.Error(LogTag, exception.ToString());
            }
        }
    }
}
#endif
