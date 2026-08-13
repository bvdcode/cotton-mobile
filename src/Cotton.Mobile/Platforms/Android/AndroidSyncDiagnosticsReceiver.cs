// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using AndroidIntentFilter = Android.App.IntentFilterAttribute;

namespace Cotton.Mobile.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [AndroidIntentFilter([Action])]
    public class AndroidSyncDiagnosticsReceiver : BroadcastReceiver
    {
        public const string Action = "dev.cottoncloud.app.debug.SYNC_DIAGNOSTICS";
        public const string OperationExtra = "operation";
        public const string RequestIdExtra = "request-id";
        public const string ScanOperation = "scan-media";
        public const string ScheduleOperation = "schedule-work";
        public const string LogTag = "CottonSyncDiagnostics";

        private const string DiagnosticInstanceUri = "https://app.cottoncloud.dev";
        private const string DiagnosticAccountScope = "android-runtime-diagnostics";
        private const string DiagnosticRootName = "Android media";
        private static readonly Guid DiagnosticRootId =
            Guid.Parse("ad000001-0000-0000-0000-000000000001");
        private static readonly Guid DiagnosticFolderId =
            Guid.Parse("ad000002-0000-0000-0000-000000000002");

        public override void OnReceive(Context? context, Intent? intent)
        {
            BroadcastReceiver.PendingResult pendingResult = GoAsync()
                ?? throw new InvalidOperationException("Android diagnostics broadcast result is unavailable.");
            _ = ExecuteAsync(intent, pendingResult);
        }

        private static async Task ExecuteAsync(
            Intent? intent,
            BroadcastReceiver.PendingResult pendingResult)
        {
            string requestId = intent?.GetStringExtra(RequestIdExtra) ?? Guid.NewGuid().ToString("N");
            try
            {
                string operation = intent?.GetStringExtra(OperationExtra)
                    ?? throw new InvalidDataException("Android diagnostics operation is required.");
                IServiceProvider services = IPlatformApplication.Current?.Services
                    ?? throw new InvalidOperationException("Android application services are unavailable.");
                string result = operation switch
                {
                    ScanOperation => await ScanMediaAsync(services).ConfigureAwait(false),
                    ScheduleOperation => await ScheduleWorkAsync(services).ConfigureAwait(false),
                    _ => throw new InvalidDataException("Android diagnostics operation is not supported."),
                };
                _ = global::Android.Util.Log.Info(LogTag, $"{requestId}:ok:{operation}:{result}");
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error(LogTag, $"{requestId}:failed:{exception}");
            }
            finally
            {
                pendingResult.Finish();
            }
        }

        private static async Task<string> ScanMediaAsync(IServiceProvider services)
        {
            AndroidMediaStoreDeviceToCloudLocalTreeReader reader = services
                .GetRequiredService<AndroidMediaStoreDeviceToCloudLocalTreeReader>();
            Uri instanceUri = new(DiagnosticInstanceUri);
            CottonSyncRootSnapshot root = CreateMediaRoot(instanceUri);
            AndroidMediaStoreScanResult scan = await reader
                .ReadWithDiagnosticsAsync(instanceUri, root)
                .ConfigureAwait(false);
            int fileCount = scan.Content.Items.Count(item => item.ItemType == CottonFileBrowserEntryType.File);
            return $"files={fileCount},hashed={scan.Statistics.HashedFileCount},reused={scan.Statistics.ReusedHashCount}";
        }

        private static async Task<string> ScheduleWorkAsync(IServiceProvider services)
        {
            ICottonAutomaticSyncBackgroundScheduler scheduler = services
                .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
            await scheduler.ScheduleAsync().ConfigureAwait(false);
            return "scheduled=true";
        }

        private static CottonSyncRootSnapshot CreateMediaRoot(Uri instanceUri)
        {
            CottonSyncLocalRootSnapshot localRoot = new(
                CottonSyncRootStorageKind.MediaStore,
                "content://media/external/file",
                DiagnosticRootName,
                CottonSyncRootPermissionStatus.Available);
            CottonUploadDestinationSnapshot destination = new(
                DiagnosticFolderId,
                DiagnosticRootName,
                DiagnosticRootName);
            return new CottonSyncRootSnapshot(
                DiagnosticRootId,
                instanceUri,
                DiagnosticAccountScope,
                destination,
                localRoot,
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);
        }
    }
}
#endif
