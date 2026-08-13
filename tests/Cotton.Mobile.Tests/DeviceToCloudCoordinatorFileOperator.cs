using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;

namespace Cotton.Mobile.Tests
{
    internal class DeviceToCloudCoordinatorFileOperator : ICottonDeviceToCloudSyncFileOperator
    {
        private readonly Dictionary<string, (Guid FileId, string ETag)> _uploadResults =
            new(StringComparer.Ordinal);

        public List<CottonDeviceToCloudSyncPlanItem> UploadedItems { get; } = [];

        public void SetUploadResult(string relativePath, Guid fileId, string eTag)
        {
            _uploadResults[relativePath] = (fileId, eTag);
        }

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            Guid operationId = item.UploadOperationId
                ?? throw new InvalidOperationException("Upload operation id was not assigned.");
            (Guid fileId, string eTag) = _uploadResults[item.RelativePath];
            Dictionary<string, string> metadata = new(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.ToString("N"),
            };
            UploadedItems.Add(item);
            return Task.FromResult(CreateFile(fileId, item.DisplayName, eTag, metadata));
        }

        public Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Folder creation is not used by these tests.");
        }
    }
}
