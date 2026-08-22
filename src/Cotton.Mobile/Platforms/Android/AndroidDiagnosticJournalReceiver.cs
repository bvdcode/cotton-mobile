// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Util;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using AndroidIntentFilter = Android.App.IntentFilterAttribute;

namespace Cotton.Mobile.Platforms.Android
{
    [BroadcastReceiver(
        Name = ComponentName,
        Enabled = true,
        Exported = true,
        Permission = DumpPermission)]
    [AndroidIntentFilter([Action])]
    public class AndroidDiagnosticJournalReceiver : BroadcastReceiver
    {
        public const string ComponentName = "dev.cottoncloud.mobile.AndroidDiagnosticJournalReceiver";
        public const string Action = "dev.cottoncloud.app.DUMP_DIAGNOSTICS";
        public const string LogTag = "CottonDiagnostics";

        private const string DumpPermission = "android.permission.DUMP";

        public override void OnReceive(Context? context, Intent? intent)
        {
            IServiceProvider services = IPlatformApplication.Current?.Services
                ?? throw new InvalidOperationException("Android application services are unavailable.");
            ICottonDiagnosticJournal journal = services.GetRequiredService<ICottonDiagnosticJournal>();
            IReadOnlyList<string> records = journal.ReadAll();
            _ = Log.Info(LogTag, $"BEGIN records={records.Count}");
            foreach (string record in records)
            {
                _ = Log.Info(LogTag, record);
            }

            _ = Log.Info(LogTag, "END");
        }
    }
}
#endif
