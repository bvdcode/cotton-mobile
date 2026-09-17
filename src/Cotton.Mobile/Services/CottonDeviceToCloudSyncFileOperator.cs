// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncFileOperator(
        ICottonFileUploadService uploadService,
        CottonSyncFileUploadSourceFactory sourceFactory,
        ICottonFileBrowserService fileBrowserService) :
        ICottonDeviceToCloudSyncFileOperator
    {
        private readonly ICottonFileUploadService _uploadService =
            uploadService ?? throw new ArgumentNullException(nameof(uploadService));
        private readonly CottonSyncFileUploadSourceFactory _sourceFactory =
            sourceFactory ?? throw new ArgumentNullException(nameof(sourceFactory));
        private readonly ICottonFileBrowserService _fileBrowserService =
            fileBrowserService ?? throw new ArgumentNullException(nameof(fileBrowserService));

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            IProgress<long>? progress,
            CancellationToken cancellationToken = default)
        {
            EnsureUploadItem(instanceUri, root, item);
            ArgumentNullException.ThrowIfNull(parentFolder);

            return _uploadService.UploadAsync(
                instanceUri,
                parentFolder,
                _sourceFactory.Create(instanceUri, root, item),
                progress,
                cancellationToken);
        }

        public Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            EnsureRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(parentFolder);
            if (!item.RequiresRemoteFolderCreate || item.TargetType != CottonFileBrowserEntryType.Folder)
            {
                throw new InvalidOperationException("Only device-to-cloud folder creation items can create folders.");
            }

            return _fileBrowserService.CreateFolderAsync(
                instanceUri,
                parentFolder,
                item.DisplayName,
                cancellationToken);
        }

        public async Task<bool> MatchesExpectedRemoteFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            EnsureDeleteItem(instanceUri, root, item);
            ArgumentNullException.ThrowIfNull(parentFolder);

            CottonFolderContent folder = await _fileBrowserService
                .GetFolderAsync(instanceUri, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            CottonFileBrowserEntry? remoteFile = folder.Entries.FirstOrDefault(entry => entry.Id == item.CloudItemId);
            return remoteFile is not null
                && remoteFile.Type == CottonFileBrowserEntryType.File
                && string.Equals(remoteFile.ETag, item.ExpectedRemoteETag, StringComparison.Ordinal)
                && remoteFile.SizeBytes == item.SizeBytes
                && string.Equals(remoteFile.ContentHash, item.ContentHash, StringComparison.Ordinal);
        }

        private static void EnsureUploadItem(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            EnsureRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(item);
            if (!item.RequiresUpload || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only device-to-cloud upload file items can upload local content.");
            }

            if (string.IsNullOrWhiteSpace(item.LocalSourceId))
            {
                throw new InvalidOperationException("Device-to-cloud upload item is missing local content.");
            }
        }

        private static void EnsureDeleteItem(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            EnsureRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(item);
            if (!item.RequiresLocalDelete
                || item.TargetType != CottonFileBrowserEntryType.File
                || !item.CloudItemId.HasValue
                || string.IsNullOrWhiteSpace(item.ExpectedRemoteETag)
                || !item.SizeBytes.HasValue
                || item.ContentHash is null)
            {
                throw new InvalidOperationException(
                    "Local cleanup requires a complete expected remote file revision.");
            }
        }

        private static void EnsureRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);

            if (!string.Equals(
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri),
                CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!CottonDeviceToCloudSyncRootCapability.CanRun(root))
            {
                throw new InvalidOperationException("Device-to-cloud sync root is not runnable.");
            }
        }
    }
}
