using System.Globalization;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncFileOperator : ICottonDeviceToCloudSyncFileOperator
    {
        private const string MetadataSourceValue = "device-to-cloud-sync";

        private readonly ICottonFileUploadService _uploadService;
        private readonly ICottonDeviceToCloudLocalFileContentSource _localContentSource;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ICottonClientFactory _clientFactory;

        public CottonDeviceToCloudSyncFileOperator(
            ICottonFileUploadService uploadService,
            ICottonDeviceToCloudLocalFileContentSource localContentSource,
            ICottonFileBrowserService fileBrowserService,
            ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(uploadService);
            ArgumentNullException.ThrowIfNull(localContentSource);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(clientFactory);

            _uploadService = uploadService;
            _localContentSource = localContentSource;
            _fileBrowserService = fileBrowserService;
            _clientFactory = clientFactory;
        }

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

        public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            EnsureUploadItem(instanceUri, root, item);
            ArgumentNullException.ThrowIfNull(parentFolder);

            return _uploadService.UpdateContentAsync(
                instanceUri,
                GetRequiredCloudItemId(item),
                parentFolder,
                GetRequiredExpectedETag(item),
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

        public async Task DeleteRemoteFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(item);
            if (!item.RequiresRemoteDelete || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only device-to-cloud remote-delete file items can delete files.");
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await client.Files
                .DeleteAsync(
                    GetRequiredCloudItemId(item),
                    skipTrash: false,
                    GetRequiredExpectedETag(item),
                    cancellationToken)
                .ConfigureAwait(false);
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
                    CreateUploadMetadata(item)),
                token => _localContentSource.OpenReadAsync(instanceUri, root, item, token));
        }

        private static IReadOnlyDictionary<string, string> CreateUploadMetadata(
            CottonDeviceToCloudSyncPlanItem item)
        {
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = MetadataSourceValue,
            };
            if (item.LocalUpdatedAtUtc.HasValue)
            {
                metadata[CottonFileUploadMetadataKeys.OriginalLastModifiedUtc] =
                    item.LocalUpdatedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
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

        private static Guid GetRequiredCloudItemId(CottonDeviceToCloudSyncPlanItem item)
        {
            return item.CloudItemId
                ?? throw new InvalidOperationException("Device-to-cloud sync item requires a cloud item id.");
        }

        private static string GetRequiredExpectedETag(CottonDeviceToCloudSyncPlanItem item)
        {
            return string.IsNullOrWhiteSpace(item.ExpectedRemoteETag)
                ? throw new InvalidOperationException("Device-to-cloud sync item requires an expected remote ETag.")
                : item.ExpectedRemoteETag;
        }
    }
}
