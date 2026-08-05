using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class BidirectionalDeviceToCloudFileOperator : ICottonDeviceToCloudSyncFileOperator
    {
        public Dictionary<string, CottonFileBrowserEntry> UploadedNewFiles { get; } =
            new(StringComparer.Ordinal);

        public List<string> UploadedNewRelativePaths { get; } = [];

        public List<string?> UploadedLocalSourceIds { get; } = [];

        public List<Guid> DeletedFileIds { get; } = [];

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            UploadedNewRelativePaths.Add(item.RelativePath);
            UploadedLocalSourceIds.Add(item.LocalSourceId);
            return Task.FromResult(UploadedNewFiles[item.RelativePath]);
        }

        public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Changed uploads are not used by these tests.");
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

        public Task DeleteRemoteFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            if (item.CloudItemId.HasValue)
            {
                DeletedFileIds.Add(item.CloudItemId.Value);
            }

            return Task.CompletedTask;
        }
    }
}
