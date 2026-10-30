// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncFileOperator(
        ICottonFileUploadService uploadService,
        ICottonDeviceToCloudLocalFileContentSource localContentSource,
        ICottonFileBrowserService fileBrowserService) :
        ICottonDeviceToCloudSyncFileOperator
    {
        private const string MetadataSourceValue = "device-to-cloud-sync";

        private readonly ICottonFileUploadService _uploadService =
            uploadService ?? throw new ArgumentNullException(nameof(uploadService));
        private readonly ICottonDeviceToCloudLocalFileContentSource _localContentSource =
            localContentSource ?? throw new ArgumentNullException(nameof(localContentSource));
        private readonly ICottonFileBrowserService _fileBrowserService =
            fileBrowserService ?? throw new ArgumentNullException(nameof(fileBrowserService));

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            EnsureUploadItem(instanceUri, root, item);
            ArgumentNullException.ThrowIfNull(parentFolder);

            return _uploadService.UploadAsync(
                instanceUri,
                parentFolder,
                CreateUploadSource(instanceUri, root, item),
                progress: null,
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

        private CottonFileUploadSource CreateUploadSource(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            return new CottonFileUploadSource(
                new CottonFileUploadSourceSnapshot(
                    item.DisplayName,
                    item.ContentType,
                    item.SizeBytes,
                    CreateUploadMetadata(item),
                    item.ContentHash),
                token => _localContentSource.OpenReadAsync(instanceUri, root, item, token));
        }

        private static Dictionary<string, string> CreateUploadMetadata(
            CottonDeviceToCloudSyncPlanItem item)
        {
            Dictionary<string, string> metadata = new(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = MetadataSourceValue,
            };
            if (item.LocalUpdatedAtUtc.HasValue)
            {
                metadata[CottonFileUploadMetadataKeys.OriginalLastModifiedUtc] =
                    item.LocalUpdatedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
            }

            if (item.UploadOperationId.HasValue)
            {
                metadata[CottonFileUploadMetadataKeys.UploadOperationId] =
                    item.UploadOperationId.Value.ToString("N");
            }

            return metadata;
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
