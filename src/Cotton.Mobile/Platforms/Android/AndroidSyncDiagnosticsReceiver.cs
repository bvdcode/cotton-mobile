// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using AndroidIntentFilter = Android.App.IntentFilterAttribute;

namespace Cotton.Mobile.Platforms.Android
{
    [BroadcastReceiver(Name = ComponentName, Enabled = true, Exported = true)]
    [AndroidIntentFilter([Action])]
    public class AndroidSyncDiagnosticsReceiver : BroadcastReceiver
    {
        public const string ComponentName = "dev.cottoncloud.app.debug.SyncDiagnosticsReceiver";
        public const string Action = "dev.cottoncloud.app.debug.SYNC_DIAGNOSTICS";
        public const string OperationExtra = "operation";
        public const string RequestIdExtra = "request-id";
        public const string ScanOperation = "scan-media";
        public const string ScheduleOperation = "schedule-work";
        public const string LogTag = "CottonSyncDiagnostics";

        private const string DiagnosticInstanceUri = "https://app.cottoncloud.dev";
        private const string DiagnosticAccountScope = "android-runtime-diagnostics";
        private const string DiagnosticAlbumName = "CottonRuntime";
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
            AndroidMediaReadAccessSnapshot access = AndroidMediaReadAccessResolver.Resolve();
            string accessMetrics =
                $"access={Convert.ToInt32(access.HasAccess)},limited={Convert.ToInt32(access.HasLimitedAccess)}";
            if (!access.HasAccess)
            {
                return $"{accessMetrics},files=0,hashed=0,reused=0";
            }

            IReadOnlyList<CottonMediaAlbumSnapshot> albums = await AndroidMediaStoreAlbumProvider
                .LoadAsync(access)
                .ConfigureAwait(false);
            CottonMediaAlbumSnapshot[] selectedAlbums = [.. albums.Where(album => string.Equals(
                album.DisplayName,
                DiagnosticAlbumName,
                StringComparison.Ordinal))];
            if (selectedAlbums.Length != 1)
            {
                throw new InvalidDataException("Android diagnostics media folder is unavailable or ambiguous.");
            }

            AndroidMediaStoreDeviceToCloudLocalTreeReader reader = services
                .GetRequiredService<AndroidMediaStoreDeviceToCloudLocalTreeReader>();
            Uri instanceUri = new(DiagnosticInstanceUri);
            CottonSyncRootSnapshot root = CreateMediaRoot(
                instanceUri,
                selectedAlbums.Select(album => album.Id));
            AndroidMediaStoreScanResult scan = await reader
                .ReadWithDiagnosticsAsync(instanceUri, root)
                .ConfigureAwait(false);
            int fileCount = scan.Content.Items.Count(item => item.ItemType == CottonFileBrowserEntryType.File);
            return $"{accessMetrics},files={fileCount},hashed={scan.Statistics.HashedFileCount},reused={scan.Statistics.ReusedHashCount}";
        }

        private static async Task<string> ScheduleWorkAsync(IServiceProvider services)
        {
            ICottonAutomaticSyncBackgroundScheduler scheduler = services
                .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
            await scheduler.ScheduleAsync().ConfigureAwait(false);
            return "scheduled=true";
        }

        private static CottonSyncRootSnapshot CreateMediaRoot(
            Uri instanceUri,
            IEnumerable<long> bucketIds)
        {
            CottonSyncLocalRootSnapshot localRoot = new(
                CottonSyncRootStorageKind.MediaStore,
                AndroidMediaStoreRootKey.Value,
                DiagnosticRootName,
                CottonSyncRootPermissionStatus.Available,
                AndroidMediaStoreScopeKey.Create(bucketIds));
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
