// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cotton.Mobile.Platforms.Android
{
    public class ConflictReviewUiScenario :
        ICottonDeviceToCloudLocalTreeReader, ICottonDeviceToCloudRemoteFolderContentSource,
        ICottonDeviceToCloudLocalFileContentSource, ICottonFileUploadService,
        ICottonUploadReceiptPathProvider, ICottonAutomaticSyncBackgroundScheduler
    {
        private readonly string _directory = Path.Combine(FileSystem.CacheDirectory, "conflict-review", Guid.NewGuid().ToString("N"));
        private readonly List<CottonDeviceToCloudLocalItemSnapshot> _local = [];
        private readonly List<CottonFileBrowserEntry> _remote = [];

        private ConflictReviewUiScenario()
        {
            string localHash = new('a', 64);
            string remoteHash = new('b', 64);
            string[] names = ["20260920_130854.heic", "20260223_110330.mp4", "Family photo with a long descriptive name.jpg"];
            long[] localSizes = [1289735, 242151328, 3499172];
            long[] remoteSizes = [1289735, 241151328, 3511172];
            DateTime modified = new(2026, 9, 20, 20, 8, 54, DateTimeKind.Utc);
            for (int index = 0; index < names.Length; index++)
            {
                _local.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(names[index], names[index], modified,
                    localSizes[index], "image/jpeg", $"content://photos/{index}", localHash));
                _remote.Add(CottonFileBrowserEntryFactory.CreateFile(Guid.NewGuid(), names[index], modified.AddMinutes(3),
                    remoteSizes[index], "image/jpeg", null, $"revision-{index}", contentHash: remoteHash));
            }
        }

        public static async Task ShowAsync(IServiceProvider services)
        {
            try
            {
                ConflictReviewUiScenario fixture = new();
                CottonSyncRootSnapshot root = new(Guid.NewGuid(), new Uri("https://upload-test.invalid"), "review-tests",
                    new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Camera backups", "/Camera"),
                    new CottonSyncLocalRootSnapshot(CottonSyncRootStorageKind.UserSelectedDocumentTree,
                        "content://documents/camera", "Camera", CottonSyncRootPermissionStatus.Available),
                    CottonSyncDirection.DeviceToCloud, CottonUploadOriginalRetention.KeepOriginals);
                using FileSystemCottonUploadReceiptStore receipts = new(fixture);
                using FileSystemCottonSyncReviewStore review = new(fixture);
                CottonSyncProgressHub progress = new();
                CottonCloudFileReplacement replacement = new(fixture, fixture, new CottonSyncFileUploadSourceFactory(fixture),
                    receipts, TimeProvider.System, NullLogger<CottonCloudFileReplacement>.Instance);
                CottonRemoteConflictResolutionService service = new(fixture, new CottonRecursiveRemoteContentLoader(fixture),
                    receipts, review, replacement, new CottonSyncRootExecutionLock(), progress, TimeProvider.System,
                    NullLogger<CottonRemoteConflictResolutionService>.Instance);
                ConflictReviewNavigation navigation = new(service, fixture, services.GetRequiredService<IUserDialogService>(),
                    services.GetRequiredService<ILoggerFactory>(), progress, services.GetRequiredService<ICottonAutomaticSyncStatusStore>());
                await navigation.ShowAsync(root, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error("CottonUploadUiTests", $"conflict-review:failed:{exception}");
            }
        }

        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return _directory;
        }

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri, CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CottonDeviceToCloudLocalContentSnapshot("Camera", _local));
        }

        public Task<CottonFolderContent> LoadAsync(Uri instanceUri, CottonFolderHandle folder, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CottonFolderContent(folder.Id, folder.Name, _remote));
        }

        public Task<Stream> OpenReadAsync(Uri instanceUri, CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CottonFileBrowserEntry> UploadAsync(Uri instanceUri, CottonFolderHandle folder,
            CottonFileUploadSource source, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CottonFileBrowserEntry> UpdateContentAsync(Uri instanceUri, Guid fileId,
            CottonFolderHandle folder, string expectedETag, CottonFileUploadSource source,
            IProgress<long>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ScheduleAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RescheduleMediaStoreTriggerAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CancelAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ScheduleRootRetriesAsync(IReadOnlyCollection<Guid> rootIds, CancellationToken cancellationToken = default)
        {
            _ = global::Android.Util.Log.Info("CottonUploadUiTests", "conflict-review:selection-saved");
            return Task.CompletedTask;
        }
    }
}
#endif
