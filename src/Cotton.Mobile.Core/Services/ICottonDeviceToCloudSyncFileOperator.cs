namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceToCloudSyncFileOperator
    {
        Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default);

        Task<CottonFileBrowserEntry> UploadChangedFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default);

        Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default);

        Task DeleteRemoteFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default);
    }
}
