using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FakeDeviceToCloudFileOperator : ICottonDeviceToCloudSyncFileOperator
    {
        public Dictionary<string, CottonFileBrowserEntry> UploadedNewFiles { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, CottonFileBrowserEntry> UploadedChangedFiles { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, CottonFileBrowserEntry> CreatedFolders { get; } =
            new(StringComparer.Ordinal);

        public List<UploadCall> NewUploadCalls { get; } = [];

        public List<UpdateCall> ChangedUploadCalls { get; } = [];

        public List<FolderCreateCall> FolderCreateCalls { get; } = [];

        public List<Guid> DeletedFileIds { get; } = [];

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            NewUploadCalls.Add(new UploadCall(instanceUri, item, parentFolder));
            return Task.FromResult(UploadedNewFiles[item.RelativePath]);
        }

        public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            ChangedUploadCalls.Add(new UpdateCall(instanceUri, item));
            return Task.FromResult(UploadedChangedFiles[item.RelativePath]);
        }

        public Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            FolderCreateCalls.Add(new FolderCreateCall(instanceUri, item, parentFolder));
            return Task.FromResult(CreatedFolders[item.RelativePath]);
        }

        public Task DeleteRemoteFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            DeletedFileIds.Add(item.CloudItemId!.Value);
            return Task.CompletedTask;
        }
    }
}
